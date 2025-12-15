using UnityEngine;
using StarterAssets;
using System.Reflection;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    [Header("设置")]
    public float interactionDistance = 10.0f;
    private const string ignoreLayerName = "Player";
    private int finalLayerMask;
    private ImageExhibition lastFrameItem;

    private void Start()
    {
        int playerLayerIndex = LayerMask.NameToLayer(ignoreLayerName);
        if (playerLayerIndex != -1) finalLayerMask = ~(1 << playerLayerIndex);
        else finalLayerMask = ~0;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 如果标记为需要恢复，启动协程
        if (GameDate.ShouldRestorePosition)
        {
            StartCoroutine(RestorePlayerState());
        }
    }

    // --- 【核心修复】延迟恢复协程 ---
    IEnumerator RestorePlayerState()
    {
        // 1. 等待一帧，让 SwitchViews 的 Start() 先跑完
        yield return new WaitForEndOfFrame();

        Debug.Log(">>> 开始执行延迟恢复...");

        // 2. 获取组件
        CharacterController cc = GetComponent<CharacterController>();
        SwitchViews switchView = GetComponent<SwitchViews>();

        // 3. 【先】暂时关闭 CC，防止它抵抗位置变化
        if (cc != null) cc.enabled = false;

        // 4. 【次】恢复视角 (这步内部可能会动位置，所以要在设置位置之前做)
        if (switchView != null)
        {
            Debug.Log($"正在恢复视角为: {(GameDate.WasFirstPerson ? "第一人称" : "第三人称")}");
            switchView.ForceSwitch(GameDate.WasFirstPerson);
        }

        // 5. 【后】强行覆盖位置 (这是防止位置偏移的关键！)
        // 无论前面发生了什么，这里把位置强制钉死在保存点
        transform.position = GameDate.LastPlayerPosition;
        transform.rotation = GameDate.LastPlayerRotation;

        // 6. 【补】反射修复内部 Yaw (防止视角回弹)
        float targetYaw = GameDate.LastPlayerRotation.eulerAngles.y;
        SyncInternalYaw(targetYaw);

        Debug.Log($"最终位置已恢复至: {transform.position}");

        // 7. 【终】重新开启 CC
        if (cc != null) cc.enabled = true;

        // 关闭开关
        GameDate.ShouldRestorePosition = false;
    }

    private void Update()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        Color debugColor = Color.yellow;

        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            // 使用 GetComponentInParent 确保能检测到子物体
            ImageExhibition itemScript = hit.collider.GetComponentInParent<ImageExhibition>();
            if (itemScript != null)
            {
                debugColor = Color.red;
                if (lastFrameItem != itemScript)
                {
                    if (lastFrameItem != null) lastFrameItem.SetHighlight(false);
                    itemScript.SetHighlight(true);
                    lastFrameItem = itemScript;
                }

                // 兼容 E 键和鼠标左键
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    itemScript.StartDisplay();
                }
            }
            else { ClearHighlight(); }
        }
        else { ClearHighlight(); }

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, debugColor);
    }

    private void ClearHighlight()
    {
        if (lastFrameItem != null) { lastFrameItem.SetHighlight(false); lastFrameItem = null; }
    }

    // 反射修复 StarterAssets 内部旋转变量
    private void SyncInternalYaw(float yaw)
    {
        MonoBehaviour controller = null;
        if (GetComponent<FirstPersonController>() != null && GetComponent<FirstPersonController>().enabled)
            controller = GetComponent<FirstPersonController>();
        else if (GetComponent<ThirdPersonController>() != null && GetComponent<ThirdPersonController>().enabled)
            controller = GetComponent<ThirdPersonController>();

        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (controller == null) controller = GetComponent<ThirdPersonController>();

        if (controller != null)
        {
            string[] possibleFieldNames = new string[] { "_cinemachineTargetYaw", "CinemachineTargetYaw", "_targetRotation" };
            bool success = false;

            foreach (var name in possibleFieldNames)
            {
                FieldInfo field = controller.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(controller, yaw);
                    success = true;
                    break;
                }
            }
            if (!success) Debug.LogWarning($"[反射警告] 无法同步视角Yaw，可能导致视角轻微回弹。");
        }
    }
}