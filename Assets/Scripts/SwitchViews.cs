using UnityEngine;
using StarterAssets;
using System.Reflection;

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
    public KeyCode switchKey = KeyCode.T;
    [Header("默认第一视角")]
    public bool startInFirstPerson = false;

    // 内部状态
    private bool isFirstPerson;

    // 缓存组件引用
    private StarterAssetsInputs fpcInput, tpcInput;
    private MonoBehaviour fpcScript, tpcScript;

    void Start()
    {
        InitializeComponents();

        // 防止 Input System 抢占，先全部关闭
        fpcRoot.SetActive(false);
        tpcRoot.SetActive(false);

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 应用初始视角
        SetViewMode(startInFirstPerson);
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SetViewMode(!isFirstPerson);
        }
    }

    // --- 核心切换逻辑 ---
    private void SetViewMode(bool toFps)
    {
        GameObject oldRoot = toFps ? tpcRoot : fpcRoot;
        GameObject newRoot = toFps ? fpcRoot : tpcRoot;
        Transform oldPlayer = toFps ? tpcPlayer : fpcPlayer;
        Transform newPlayer = toFps ? fpcPlayer : tpcPlayer;
        StarterAssetsInputs oldInput = toFps ? tpcInput : fpcInput;
        StarterAssetsInputs newInput = toFps ? fpcInput : tpcInput;

        // 关闭旧视角
        if (oldRoot.activeSelf)
        {
            oldRoot.SetActive(false);
            if (oldInput) ResetInput(oldInput);
        }

        // 计算对齐
        GetCameraAlignment(oldPlayer, out Vector3 targetPos, out float targetYaw, out float targetPitch);

        // 应用位置
        newPlayer.position = targetPos;
        newPlayer.rotation = Quaternion.Euler(0, targetYaw, 0);

        // 反射同步变量
        MonoBehaviour targetScript = toFps ? fpcScript : tpcScript;
        SyncInternalVariables(targetScript, targetYaw, targetPitch);

        // 激活新视角
        newRoot.SetActive(true);
        if (newInput) ResetInput(newInput);

        isFirstPerson = toFps;
    }

    // --- 辅助方法 ---
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
        pos = fallbackTransform.position + Vector3.up * 0.05f;
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

    private void SyncInternalVariables(MonoBehaviour script, float yaw, float pitch)
    {
        if (script == null) return;
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

    // =========================================================
    // 【关键修改】取消注释以下两个方法，供 PlayerInteraction 调用
    // =========================================================

    // 判断当前是否是第一人称
    public bool IsInFirstPerson()
    {
        // 简单判断：如果第一人称根节点是激活的，那就是第一人称
        return fpcRoot != null && fpcRoot.activeSelf;
    }

    // 强行切换到指定视角 (供恢复存档使用)
    public void ForceSwitch(bool toFirstPerson)
    {
        // 直接调用核心逻辑，不经过按键判断
        SetViewMode(toFirstPerson);
    }
}