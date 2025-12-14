using UnityEngine;
using StarterAssets;
using System.Reflection;

public class SwitchViews : MonoBehaviour
{
    [Header("第一人称视角配置")]
    [Tooltip("第一人称父节点，如One_Player")]
    public GameObject fpcRoot;
    [Tooltip("第一人称玩家模型，如PlayerCapsule")]
    public Transform fpcPlayer;
    [Tooltip("第一人称玩家相机节点，如PlayerCameraRoot")]
    public Transform fpcCameraRoot;

    [Header("第三人称视角配置")]
    [Tooltip("第三人称父节点，如Third_Player")]
    public GameObject tpcRoot;
    [Tooltip("第三人称玩家模型，如PlayerArmature")]
    public Transform tpcPlayer;
    [Tooltip("第一人称玩家相机节点，如PlayerCameraRoot")]
    public Transform tpcCameraRoot;

    [Header("快捷键设置")]
    public KeyCode switchKey = KeyCode.T;
    [Header("默认第一视角")]
    public bool startInFirstPerson = false;

    // 内部状态
    private bool isFirstPerson;

    // 缓存组件引用
    private StarterAssetsInputs fpcInput, tpcInput;
    private MonoBehaviour fpcScript, tpcScript; // 使用 MonoBehaviour 存储脚本，减少类型依赖

    void Start()
    {
        InitializeComponents();

        // 为了防止 Input System 抢占，开局先全部关闭
        fpcRoot.SetActive(false);
        tpcRoot.SetActive(false);

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 应用初始视角 (复用核心切换逻辑)
        SetViewMode(startInFirstPerson);
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SetViewMode(!isFirstPerson);
        }
    }

    /// <summary>
    /// 核心切换逻辑：设置当前是第一人称还是第三人称
    /// </summary>
    private void SetViewMode(bool toFps)
    {
        // 1. 确定 源(旧) 和 目标(新)
        GameObject oldRoot = toFps ? tpcRoot : fpcRoot;
        GameObject newRoot = toFps ? fpcRoot : tpcRoot;

        Transform oldPlayer = toFps ? tpcPlayer : fpcPlayer;
        Transform newPlayer = toFps ? fpcPlayer : tpcPlayer;

        StarterAssetsInputs oldInput = toFps ? tpcInput : fpcInput;
        StarterAssetsInputs newInput = toFps ? fpcInput : tpcInput;

        // 2. 关闭旧视角 (释放输入权)
        if (oldRoot.activeSelf)
        {
            oldRoot.SetActive(false);
            if (oldInput) ResetInput(oldInput);
        }

        // 3. 计算对齐数据 (基于主摄像机，实现"所见即所得")
        GetCameraAlignment(oldPlayer, out Vector3 targetPos, out float targetYaw, out float targetPitch);

        // 4. 应用数据到新角色 (包括物理位置和脚本内部变量)
        newPlayer.position = targetPos;
        newPlayer.rotation = Quaternion.Euler(0, targetYaw, 0); // 强制转身对齐

        // 使用反射写入私有变量，防止漂移
        MonoBehaviour targetScript = toFps ? fpcScript : tpcScript;
        SyncInternalVariables(targetScript, targetYaw, targetPitch);

        // 5. 激活新视角
        newRoot.SetActive(true);
        if (newInput) ResetInput(newInput);

        // 更新状态记录
        isFirstPerson = toFps;
    }

    // --- 辅助方法 ---

    private void InitializeComponents()
    {
        // 使用 true 参数确保即使物体隐藏也能找到组件
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
        // 位置：稍微抬高一点防止卡地
        pos = fallbackTransform.position + Vector3.up * 0.05f;

        // 旋转：优先使用主摄像机朝向，如果没有则使用旧模型朝向
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            yaw = mainCam.transform.eulerAngles.y;
            pitch = mainCam.transform.eulerAngles.x;
        }
        else
        {
            yaw = fallbackTransform.eulerAngles.y;
            pitch = 0f; // 没摄像机时默认平视
        }

        // 规范化 Pitch 角度
        if (pitch > 180) pitch -= 360;
    }

    private void SyncInternalVariables(MonoBehaviour script, float yaw, float pitch)
    {
        if (script == null) return;
        // 反射修改 StarterAssets 的私有变量
        SetPrivateField(script, "_cinemachineTargetYaw", yaw);
        SetPrivateField(script, "_cinemachineTargetPitch", pitch);
    }

    private void SetPrivateField(object target, string fieldName, float value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(target, value);
    }

    private void ResetInput(StarterAssetsInputs input)
    {
        input.move = Vector2.zero;
        input.look = Vector2.zero;
        input.jump = false;
        input.sprint = false;
        input.analogMovement = false;
    }
}