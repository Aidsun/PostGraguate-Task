using UnityEngine;
using StarterAssets;
using System.Reflection;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    [Tooltip("交互距离，默认为10（默认值，会被设置面板覆盖）")]
    public float interactionDistance = 10.0f;

    // 忽略玩家层名称（防止射线检测到自己）
    private const string ignoreLayerName = "Player";
    // 最终用于射线检测的 LayerMask
    private int finalLayerMask;
    // 上一帧被高亮的展品脚本
    private MonoBehaviour lastFrameItem;

    void Awake()
    {
        // 注册到设置面板，接收配置更新
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        // 注销设置应用方法
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    private void Start()
    {
        // 获取玩家层索引
        int playerLayerIndex = LayerMask.NameToLayer(ignoreLayerName);
        // 计算最终 LayerMask，忽略玩家层
        if (playerLayerIndex != -1) finalLayerMask = ~(1 << playerLayerIndex);
        else finalLayerMask = ~0;

        // 锁定并隐藏鼠标 (初始化时)
        // 注意：SwitchViews 也会做这件事，但这里再做一次作为双重保险
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 【关键修改】移除了 RestorePlayerPosition() 调用
        // 位置恢复工作现在全权交给 SwitchViews.cs 处理，防止逻辑冲突

        // 应用当前设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        // 应用交互距离
        interactionDistance = settings.interactionDistance;

        Debug.Log($"PlayerInteraction: 应用设置 - 交互距离: {interactionDistance}");
    }

    private void Update()
    {
        // ==========================================================
        // 【关键修复】如果控制面板打开了，直接不再进行射线检测
        // ==========================================================
        if (SettingPanel.Instance != null && SettingPanel.Instance.isPanelActive)
        {
            // 此时应该清除可能残留的高亮，保持界面干净
            ClearHighlight();
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // 从屏幕中心发射射线（使用当前的交互距离）
        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            // 查找图片展品脚本
            ImageExhibition imgScript = hit.collider.GetComponentInParent<ImageExhibition>();
            // 查找视频展品脚本
            VideoExhibition vidScript = hit.collider.GetComponentInParent<VideoExhibition>();
            // 查找全景视频展品脚本
            PanoramaExhibition pnmScript = hit.collider.GetComponentInParent<PanoramaExhibition>();

            // 展品高亮与交互逻辑
            if (imgScript != null)
            {
                HandleHighlight(imgScript, imgScript.ImageTitle);
                // 按下E键或鼠标左键进行交互
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    imgScript.StartDisplay();
            }
            else if (vidScript != null)
            {
                HandleHighlight(vidScript, vidScript.VideoTitle);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    vidScript.StartDisplay();
            }
            else if (pnmScript != null)
            {
                HandleHighlight(pnmScript, pnmScript.PanoramaTitle);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    pnmScript.StartDisplay();
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

    // 展品高亮处理（供内部调用）
    void HandleHighlight(MonoBehaviour currentItem, string itemName)
    {
        if (lastFrameItem != currentItem)
        {
            ClearHighlight(); // 先清除旧高亮

            // 设置新高亮
            if (currentItem is ImageExhibition img) img.SetHighlight(true);
            if (currentItem is VideoExhibition vid) vid.SetHighlight(true);
            if (currentItem is PanoramaExhibition pnm) pnm.SetHighlight(true);

            lastFrameItem = currentItem;
        }
    }

    // 清除高亮（供内部调用）
    private void ClearHighlight()
    {
        if (lastFrameItem != null)
        {
            if (lastFrameItem is ImageExhibition img) img.SetHighlight(false);
            if (lastFrameItem is VideoExhibition vid) vid.SetHighlight(false);
            if (lastFrameItem is PanoramaExhibition pnm) pnm.SetHighlight(false);
            lastFrameItem = null;
        }
    }
}