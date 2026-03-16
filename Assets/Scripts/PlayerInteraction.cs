// 文件：PlayerInteraction.cs
// 模块：玩家控制器 / 交互系统
// 说明：该脚本挂载在玩家角色上，负责处理玩家与场景中可交互物体的交互。
//      通过屏幕中心的射线检测来识别可交互物体（如展品、答题点），
//      控制物体的高亮显示，显示交互提示UI，并在玩家点击时触发物体的交互逻辑。
// 特性：使用Physics.Raycast进行射线检测，通过LayerMask忽略玩家层，
//      QueryTriggerInteraction.Ignore忽略触发器，防止被透明碰撞体阻挡；
//      使用SendMessage调用被交互物体的方法，实现解耦；依赖多个单例管理UI、音频等。

using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]           // 在Inspector中分组显示
    public float interactionDistance = 10.0f;   // 玩家可交互的最大距离

    // 不需要专门为了忽略 Raycast 设 Layer 了，我们用代码强制忽略 Trigger
    // 忽略的层名称，这里设置为"Player"，即玩家自身所在的层
    private const string ignoreLayerName = "Player";
    // 最终的LayerMask，用于射线检测时忽略指定层
    private int finalLayerMask;
    // 上一帧检测到的可交互物体（MonoBehaviour类型），用于清除高亮和更新状态
    private MonoBehaviour lastFrameItem;

    private void Start()
    {
        // 只忽略 Player 层，其他的我们用 QueryTriggerInteraction 来控制
        // 获取"Player"层的索引
        int playerLayer = LayerMask.NameToLayer(ignoreLayerName);
        int maskToIgnore = 0;
        if (playerLayer != -1)
            maskToIgnore |= (1 << playerLayer);   // 将玩家层的位设为1
        // 对忽略掩码取反，得到最终射线检测要使用的掩码（即除了玩家层外都检测）
        finalLayerMask = ~maskToIgnore;

        // 初始锁定鼠标，隐藏光标（第一人称游戏通常如此）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 1. 防止 UI 穿透：当设置面板打开或光标可见时，不应该进行交互检测
        //    此时清除高亮并返回
        if ((SettingPanel.Instance && SettingPanel.Instance.isPanelActive) || Cursor.visible)
        {
            ClearHighlight();
            return;
        }

        // 从GameData中获取当前设置的交互距离（允许动态调整）
        if (GameData.Instance != null)
            interactionDistance = GameData.Instance.InteractionDistance;

        // 执行射线检测
        PerformRaycast();
    }

    /// <summary>
    /// 执行屏幕中心射线检测，识别可交互物体并处理高亮和交互提示。
    /// </summary>
    private void PerformRaycast()
    {
        // 如果没有主相机，则无法进行射线检测，直接返回
        if (Camera.main == null) return;

        // 从主相机中心点发射一条射线
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // =========================================================
        // 【核心修复 1】交互失灵的救星
        // QueryTriggerInteraction.Ignore = 让射线无视所有 Trigger 触发器！
        // 这样即便你站在 TutorCube 透明盒子里，射线也能穿过去打到画框。
        // =========================================================
        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask, QueryTriggerInteraction.Ignore))
        {
            // 尝试从碰撞体上获取各种可交互组件（使用GetComponentInParent以便处理物体挂载在父级的情况）
            var img = hit.collider.GetComponentInParent<ImageExhibition>();
            var vid = hit.collider.GetComponentInParent<VideoExhibition>();
            var pnm = hit.collider.GetComponentInParent<PanoramaExhibition>();
            var quiz = hit.collider.GetComponentInParent<QuestionInteraction>();
            var guide = hit.collider.GetComponentInParent<GuideNPC>();
            var quizPoint = hit.collider.GetComponentInParent<QuizPoint>();
            string msg = "";   // 要显示的交互提示文字

            // =========================================================
            // 【核心修复 2】UI 文字显示逻辑 (之前这里是空的)
            // =========================================================
            // 根据检测到的物体类型，设置对应的提示文字，并调用HandleInteract处理
            if (img)
            {
                msg = "左键进行交互";
                HandleInteract(img, msg);
            }
            else if (vid)
            {
                msg = "左键进行交互";
                HandleInteract(vid, msg);
            }
            else if (pnm)
            {
                msg = "左键进行交互";
                HandleInteract(pnm, msg);
            }
            else if (quiz)
            {
                msg = "左键进入答题";
                HandleInteract(quiz, msg);
            }
            else if (guide)
            {
                msg = "与导览员对话";
                HandleInteract(guide,msg);
            }
            else if (quizPoint)
            {
                msg = "左键进入答题点进行答题";
                HandleInteract(quizPoint, msg);
            }
            else
            {
                // 如果射线击中的物体不是任何可交互类型，则清除高亮
                ClearHighlight();
            }
        }
        else
        {
            // 如果射线没有击中任何物体，也清除高亮
            ClearHighlight();
        }
    }

    /// <summary>
    /// 处理与可交互物体的交互逻辑，包括高亮切换、播放音效、显示提示UI，以及响应点击事件。
    /// </summary>
    /// <param name="item">被检测到的可交互物体（MonoBehaviour，实际应为ImageExhibition等）</param>
    /// <param name="message">要显示的交互提示文字</param>
    private void HandleInteract(MonoBehaviour item, string message)
    {
        // 如果当前帧的物体与上一帧不同，说明玩家聚焦到了一个新的物体上
        if (lastFrameItem != item)
        {
            // 先清除上一帧物体的高亮
            ClearHighlight();
            // 记录新的物体
            lastFrameItem = item;

            // 播放高亮音效（如果有）
            if (AudioManager.Instance) AudioManager.Instance.PlayHighlightSound();

            // 通过SendMessage调用物体上的SetHighlight方法，参数为true，表示激活高亮
            // SendMessageOptions.DontRequireReceiver表示如果没有该方法也不会报错
            item.SendMessage("SetHighlight", true, SendMessageOptions.DontRequireReceiver);

            // 【核心修复 3】呼叫 UI 显示交互提示
            if (InteractionButton.Instance != null)
            {
                InteractionButton.Instance.ShowInteractionButton(message);
            }
        }

        // 如果玩家按下鼠标左键，触发交互
        if (Input.GetMouseButtonDown(0))
        {
            // 通过SendMessage调用物体上的StartDisplay方法，启动交互逻辑（如跳转场景）
            item.SendMessage("StartDisplay", SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// 清除当前物体的高亮，并隐藏交互提示UI。
    /// </summary>
    private void ClearHighlight()
    {
        // 如果存在上一帧的物体，则调用其SetHighlight方法，参数为false，关闭高亮
        if (lastFrameItem != null)
        {
            lastFrameItem.SendMessage("SetHighlight", false, SendMessageOptions.DontRequireReceiver);
            lastFrameItem = null;
        }

        // 【核心修复 4】呼叫 UI 隐藏
        if (InteractionButton.Instance != null)
        {
            InteractionButton.Instance.HideInteractionButton();
        }
    }
}