using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private float interactionDistance = 10.0f;
    private const string ignoreLayerName_1 = "Player";
    private const string ignoreLayerName_2 = "Ignore Raycast";

    private int finalLayerMask;
    private MonoBehaviour lastFrameItem;

    private void Start()
    {
        int playerLayer = LayerMask.NameToLayer(ignoreLayerName_1);
        int ignoreRaycastLayer = LayerMask.NameToLayer(ignoreLayerName_2);
        int maskToIgnore = 0;

        if (playerLayer != -1) maskToIgnore |= (1 << playerLayer);
        if (ignoreRaycastLayer != -1) maskToIgnore |= (1 << ignoreRaycastLayer);

        finalLayerMask = ~maskToIgnore;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 【核心修复】
        // 如果设置面板打开了，或者鼠标指针是可见的（说明正在操作答题板或提示板）
        // 就直接停止射线检测，防止点穿到后面的物体
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

        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            // 获取各种交互组件
            var img = hit.collider.GetComponentInParent<ImageExhibition>();
            var vid = hit.collider.GetComponentInParent<VideoExhibition>();
            var pnm = hit.collider.GetComponentInParent<PanoramaExhibition>();

            // 【新增】检测答题交互组件
            var quiz = hit.collider.GetComponentInParent<QuestionInteraction>();

            // 优先级判断
            if (img) HandleInteract(img);
            else if (vid) HandleInteract(vid);
            else if (pnm) HandleInteract(pnm);
            else if (quiz) HandleInteract(quiz); // <--- 加入这一行
            else ClearHighlight();
        }
        else { ClearHighlight(); }
    }

    private void HandleInteract(MonoBehaviour item)
    {
        if (lastFrameItem != item)
        {
            ClearHighlight(); lastFrameItem = item;

            if (AudioManager.Instance)
                AudioManager.Instance.PlayHighlightSound();

            // 这里会调用 QuestionInteraction 里的 SetHighlight
            item.SendMessage("SetHighlight", true, SendMessageOptions.DontRequireReceiver);
        }

        // 这里会调用 QuestionInteraction 里的 StartDisplay
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            item.SendMessage("StartDisplay", SendMessageOptions.DontRequireReceiver);
    }

    private void ClearHighlight()
    {
        if (lastFrameItem != null)
        {
            lastFrameItem.SendMessage("SetHighlight", false, SendMessageOptions.DontRequireReceiver);
            lastFrameItem = null;
        }
    }
}