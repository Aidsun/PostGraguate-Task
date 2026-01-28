using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionDistance = 10.0f;

    // 不需要专门为了忽略 Raycast 设 Layer 了，我们用代码强制忽略 Trigger
    private const string ignoreLayerName = "Player";
    private int finalLayerMask;
    private MonoBehaviour lastFrameItem;

    private void Start()
    {
        // 只忽略 Player 层，其他的我们用 QueryTriggerInteraction 来控制
        int playerLayer = LayerMask.NameToLayer(ignoreLayerName);
        int maskToIgnore = 0;
        if (playerLayer != -1) maskToIgnore |= (1 << playerLayer);
        finalLayerMask = ~maskToIgnore;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 1. 防止 UI 穿透
        if ((SettingPanel.Instance && SettingPanel.Instance.isPanelActive) || Cursor.visible)
        {
            ClearHighlight();
            return;
        }

        if (GameData.Instance != null) interactionDistance = GameData.Instance.InteractionDistance;

        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // =========================================================
        // 【核心修复 1】交互失灵的救星
        // QueryTriggerInteraction.Ignore = 让射线无视所有 Trigger 触发器！
        // 这样即便你站在 TutorCube 透明盒子里，射线也能穿过去打到画框。
        // =========================================================
        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask, QueryTriggerInteraction.Ignore))
        {
            var img = hit.collider.GetComponentInParent<ImageExhibition>();
            var vid = hit.collider.GetComponentInParent<VideoExhibition>();
            var pnm = hit.collider.GetComponentInParent<PanoramaExhibition>();
            var quiz = hit.collider.GetComponentInParent<QuestionInteraction>();

            string msg = "";

            // =========================================================
            // 【核心修复 2】UI 文字显示逻辑 (之前这里是空的)
            // =========================================================
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
            else
            {
                ClearHighlight();
            }
        }
        else
        {
            ClearHighlight();
        }
    }

    // 增加了 message 参数
    private void HandleInteract(MonoBehaviour item, string message)
    {
        if (lastFrameItem != item)
        {
            ClearHighlight();
            lastFrameItem = item;

            if (AudioManager.Instance) AudioManager.Instance.PlayHighlightSound();

            // 物体发光
            item.SendMessage("SetHighlight", true, SendMessageOptions.DontRequireReceiver);

            // 【核心修复 3】呼叫 UI 显示
            if (InteractionButton.Instance != null)
            {
                InteractionButton.Instance.ShowInteractionButton(message);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            item.SendMessage("StartDisplay", SendMessageOptions.DontRequireReceiver);
        }
    }

    private void ClearHighlight()
    {
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