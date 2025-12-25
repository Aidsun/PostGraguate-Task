using UnityEngine;
using StarterAssets;
using System.Reflection;
using System; // 【关键修复】加上了这句，解决红色报错！

public class SwitchViews : MonoBehaviour
{
    [Header("第一人称视角配置")]
    public GameObject fpcRoot;
    public Transform fpcPlayer;
    public Transform fpcCameraRoot;

    [Header("第三人称视角配置")]
    public GameObject tpcRoot;
    public Transform tpcPlayer;
    public Transform tpcCameraRoot;

    [Header("快捷键设置")]
    [Tooltip("视角切换快捷键（默认值，会被设置面板覆盖）")]
    public KeyCode switchKey = KeyCode.T;

    [Header("默认第一视角")]
    [Tooltip("是否默认第一人称视角（默认值，会被设置面板覆盖）")]
    public bool startInFirstPerson = true;

    [Header("🎮 手感微调")]
    [Tooltip("固定鼠标灵敏度 (不再受设置面板控制)")]
    public float fixedSensitivity = 1.5f;

    // 缓存组件引用
    private StarterAssetsInputs fpcInput, tpcInput;
    private MonoBehaviour fpcScript, tpcScript;

    // 标志：是否已经恢复了位置
    private bool hasRestoredPosition = false;

    void Awake()
    {
        // 注册到设置面板，接收配置更新
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }

        InitializeComponents();

        // 1. 先关闭 Input，防止抢夺控制
        if (fpcRoot) fpcRoot.SetActive(false);
        if (tpcRoot) tpcRoot.SetActive(false);

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        // 注销设置应用方法
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void Start()
    {
        // 应用当前设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        // 优先检查是否需要恢复位置 (来自展品返回)
        if (GameDate.ShouldRestorePosition && !hasRestoredPosition)
        {
            Debug.Log($"检测到需要恢复位置，位置={GameDate.LastPlayerPosition}, 视角模式={GameDate.WasFirstPerson}");

            // 使用保存的视角模式
            SetViewModeWithRestoration(GameDate.WasFirstPerson);

            // 标记已恢复，防止重复执行
            hasRestoredPosition = true;
            GameDate.ShouldRestorePosition = false;
        }
        else
        {
            // 正常启动，使用设置面板的默认配置
            SetViewMode(startInFirstPerson);
            hasRestoredPosition = false;
        }
    }

    void Update()
    {
        // 监听按键切换（使用当前设置的按键）
        if (Input.GetKeyDown(switchKey))
        {
            SetViewMode(!IsInFirstPerson());
        }
    }

    // =========================================================
    // 设置应用方法 (由 SettingPanel 调用)
    // =========================================================
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        // 应用视角切换快捷键
        switchKey = settings.viewSwitchKey;

        // 应用默认视角设置 (仅影响首次进入)
        startInFirstPerson = settings.defaultFirstPersonView;

        // 应用角色控制参数
        // 灵敏度直接使用本地 fixedSensitivity
        UpdateCharacterSettings(settings.moveSpeed, settings.jumpHeight, fixedSensitivity);

        Debug.Log($"SwitchViews: 应用设置 - 切换键: {switchKey}, 固定灵敏度: {fixedSensitivity}");
    }

    // =========================================================
    // 核心切换逻辑
    // =========================================================

    // 普通切换
    private void SetViewMode(bool toFps)
    {
        SetViewModeInternal(toFps, false);
    }

    // 带位置恢复的切换
    private void SetViewModeWithRestoration(bool toFps)
    {
        SetViewModeInternal(toFps, true);
    }

    private void SetViewModeInternal(bool toFps, bool isRestoring)
    {
        if (fpcRoot == null || tpcRoot == null) return;

        GameObject oldRoot = toFps ? tpcRoot : fpcRoot;
        GameObject newRoot = toFps ? fpcRoot : tpcRoot;
        Transform oldPlayer = toFps ? tpcPlayer : fpcPlayer;
        Transform newPlayer = toFps ? fpcPlayer : tpcPlayer;
        StarterAssetsInputs oldInput = toFps ? tpcInput : fpcInput;
        StarterAssetsInputs newInput = toFps ? fpcInput : tpcInput;

        // 如果新旧是同一个且已经激活，直接返回，避免重复操作
        if (oldRoot == newRoot && oldRoot.activeSelf) return;

        // 关闭旧的
        if (oldRoot.activeSelf)
        {
            oldRoot.SetActive(false);
            if (oldInput) ResetInput(oldInput);
        }

        if (isRestoring)
        {
            // --- 恢复模式 ---
            Transform activePlayer = GetActivePlayerTransform();
            if (activePlayer != null)
            {
                // 临时禁用CharacterController以便设置位置 (防止瞬移失效)
                CharacterController cc = newPlayer.GetComponent<CharacterController>();
                bool wasEnabled = false;
                if (cc != null)
                {
                    wasEnabled = cc.enabled;
                    cc.enabled = false;
                }

                // 还原位置
                newPlayer.position = GameDate.LastPlayerPosition;
                newPlayer.rotation = GameDate.LastPlayerRotation;

                // 还原CC
                if (cc != null && wasEnabled) cc.enabled = true;

                // 同步相机角度
                MonoBehaviour targetScript = toFps ? fpcScript : tpcScript;
                SyncCameraRotation(targetScript, GameDate.LastPlayerRotation);
            }
        }
        else
        {
            // --- 正常模式：平滑过渡位置 ---
            GetCameraAlignment(oldPlayer, out Vector3 targetPos, out float targetYaw, out float targetPitch);

            newPlayer.position = targetPos;
            newPlayer.rotation = Quaternion.Euler(0, targetYaw, 0);

            MonoBehaviour targetScript = toFps ? fpcScript : tpcScript;
            SyncInternalVariables(targetScript, targetYaw, targetPitch);
        }

        // 切换后再次强制应用一遍参数，确保速度/灵敏度正确
        if (SettingPanel.Instance != null)
        {
            UpdateCharacterSettings(
                SettingPanel.CurrentSettings.moveSpeed,
                SettingPanel.CurrentSettings.jumpHeight,
                fixedSensitivity
            );
        }

        // 激活新的
        newRoot.SetActive(true);
        if (newInput) ResetInput(newInput);
    }

    // =========================================================
    // 参数同步逻辑
    // =========================================================

    public void UpdateCharacterSettings(float moveSpeed, float jumpHeight, float sensitivity)
    {
        // 只更新当前激活的那个控制器
        MonoBehaviour activeScript = IsInFirstPerson() ? fpcScript : tpcScript;

        if (activeScript != null)
        {
            SetPublicField(activeScript, "MoveSpeed", moveSpeed);
            SetPublicField(activeScript, "SprintSpeed", moveSpeed * 1.5f);
            SetPublicField(activeScript, "JumpHeight", jumpHeight);

            // 灵敏度处理：第三人称通常需要更大的数值倍率
            float rotSpeed = IsInFirstPerson() ? sensitivity : sensitivity * 100f;
            SetPublicField(activeScript, "RotationSpeed", rotSpeed);
        }
    }

    // =========================================================
    // 辅助工具方法
    // =========================================================

    public bool IsInFirstPerson()
    {
        return fpcRoot != null && fpcRoot.activeSelf;
    }

    public Transform GetActivePlayerTransform()
    {
        return IsInFirstPerson() ? (fpcPlayer ? fpcPlayer : transform) : (tpcPlayer ? tpcPlayer : transform);
    }

    private void InitializeComponents()
    {
        if (fpcRoot)
        {
            fpcInput = fpcRoot.GetComponentInChildren<StarterAssetsInputs>(true);
            fpcScript = fpcRoot.GetComponentInChildren<FirstPersonController>(true);
        }
        if (tpcRoot)
        {
            tpcInput = tpcRoot.GetComponentInChildren<StarterAssetsInputs>(true);
            tpcScript = tpcRoot.GetComponentInChildren<ThirdPersonController>(true);
        }
    }

    private void GetCameraAlignment(Transform fallbackTransform, out Vector3 pos, out float yaw, out float pitch)
    {
        pos = fallbackTransform.position;
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            yaw = mainCam.transform.eulerAngles.y;
            pitch = mainCam.transform.eulerAngles.x;
        }
        else
        {
            yaw = fallbackTransform.eulerAngles.y;
            pitch = 0f;
        }
        if (pitch > 180) pitch -= 360;
    }

    private void SyncCameraRotation(MonoBehaviour script, Quaternion rotation)
    {
        if (script == null) return;
        float yaw = rotation.eulerAngles.y;
        float pitch = rotation.eulerAngles.x;
        if (pitch > 180) pitch -= 360;
        SyncInternalVariables(script, yaw, pitch);
    }

    private void SyncInternalVariables(MonoBehaviour script, float yaw, float pitch)
    {
        if (script == null) return;
        // 尝试设置多种可能的变量名
        SetPrivateField(script, "_cinemachineTargetYaw", yaw);
        SetPrivateField(script, "_cinemachineTargetPitch", pitch);
        SetPrivateField(script, "CinemachineTargetYaw", yaw);
        SetPrivateField(script, "CinemachineTargetPitch", pitch);
    }

    private void SetPrivateField(object target, string fieldName, float value)
    {
        if (target == null) return;
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(target, value);
    }

    // 【强力修复版】既找Public，也找Private，还找属性，甚至尝试首字母小写
    private void SetPublicField(object target, string fieldName, float value)
    {
        if (target == null) return;

        // 【关键】这里用到的 Type 类需要 using System; 顶部已添加
        Type type = target.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // 1. 尝试找字段 (Field)
        FieldInfo field = type.GetField(fieldName, flags);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        // 2. 尝试找属性 (Property)
        PropertyInfo prop = type.GetProperty(fieldName, flags);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
            return;
        }

        // 3. 尝试首字母小写 (防变量名变体)
        string lowerName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
        FieldInfo lowerField = type.GetField(lowerName, flags);
        if (lowerField != null)
        {
            lowerField.SetValue(target, value);
            return;
        }

        // 屏蔽烦人的警告，如果真的找不到就算了，不影响运行
        // Debug.LogWarning($"[SwitchViews] 未找到变量 {fieldName}"); 
    }

    private void ResetInput(StarterAssetsInputs input)
    {
        if (input == null) return;
        input.move = Vector2.zero;
        input.look = Vector2.zero;
        input.jump = false;
        input.sprint = false;
        input.analogMovement = false;
    }
}