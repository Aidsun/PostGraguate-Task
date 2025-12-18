using UnityEngine;
using StarterAssets;
using System.Reflection;

public class PlayerInteraction : MonoBehaviour
{
    [Header("设置")]
    public float interactionDistance = 10.0f;
    private const string ignoreLayerName = "Player";
    private int finalLayerMask;

    // 【修改】改成 MonoBehaviour，以便同时支持图片和视频脚本
    private MonoBehaviour lastFrameItem;

    private void Start()
    {
        int playerLayerIndex = LayerMask.NameToLayer(ignoreLayerName);
        if (playerLayerIndex != -1) finalLayerMask = ~(1 << playerLayerIndex);
        else finalLayerMask = ~0;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 立即恢复位置
        if (GameDate.ShouldRestorePosition)
        {
            RestorePlayerPosition();
        }
    }

    void RestorePlayerPosition()
    {
        Debug.Log($">>> [立即恢复] 目标位置: {GameDate.LastPlayerPosition}");
        SwitchViews switchScript = GetComponent<SwitchViews>();
        if (switchScript != null)
        {
            Transform activePlayer = switchScript.GetActivePlayerTransform();
            CharacterController cc = activePlayer.GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false;

            activePlayer.position = GameDate.LastPlayerPosition;
            activePlayer.rotation = GameDate.LastPlayerRotation;

            // 修复视角
            float targetYaw = GameDate.LastPlayerRotation.eulerAngles.y;
            SyncInternalYaw(activePlayer.gameObject, targetYaw);

            Physics.SyncTransforms();

            if (cc != null) cc.enabled = true;

            Debug.Log($"[恢复完毕] 无缝衔接成功！");
        }
        else
        {
            Debug.LogError("PlayerInteraction 未能找到 SwitchViews 组件！");
        }
        GameDate.ShouldRestorePosition = false;
    }

    private void Update()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            // 1. 尝试找图片脚本
            ImageExhibition imgScript = hit.collider.GetComponentInParent<ImageExhibition>();
            // 2. 尝试找视频脚本
            VideoExhibition vidScript = hit.collider.GetComponentInParent<VideoExhibition>();

            // --- 统一逻辑 ---
            if (imgScript != null)
            {
                HandleHighlight(imgScript, imgScript.ImageTitle);
                // 交互
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    imgScript.StartDisplay();
            }
            else if (vidScript != null)
            {
                HandleHighlight(vidScript, vidScript.VideoTitle);
                // 交互
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                    vidScript.StartDisplay();
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

    // 通用高亮处理方法
    void HandleHighlight(MonoBehaviour currentItem, string itemName)
    {
        if (lastFrameItem != currentItem)
        {
            ClearHighlight(); // 先清除旧的

            // 开启新的
            if (currentItem is ImageExhibition img) img.SetHighlight(true);
            if (currentItem is VideoExhibition vid) vid.SetHighlight(true);

            lastFrameItem = currentItem;
            // Debug.Log($"瞄准了: {itemName}");
        }
    }

    private void ClearHighlight()
    {
        if (lastFrameItem != null)
        {
            if (lastFrameItem is ImageExhibition img) img.SetHighlight(false);
            if (lastFrameItem is VideoExhibition vid) vid.SetHighlight(false);
            lastFrameItem = null;
        }
    }

    private void SyncInternalYaw(GameObject playerObj, float yaw)
    {
        MonoBehaviour controller = null;
        if (playerObj.GetComponent<FirstPersonController>() != null)
            controller = playerObj.GetComponent<FirstPersonController>();
        else if (playerObj.GetComponent<ThirdPersonController>() != null)
            controller = playerObj.GetComponent<ThirdPersonController>();

        if (controller != null)
        {
            string[] possibleFieldNames = new string[] { "_cinemachineTargetYaw", "CinemachineTargetYaw", "_targetRotation" };
            foreach (var name in possibleFieldNames)
            {
                FieldInfo field = controller.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(controller, yaw);
                    break;
                }
            }
        }
    }
}