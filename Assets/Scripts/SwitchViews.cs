using UnityEngine;
using StarterAssets;
using System.Reflection;

public class SwitchViews : MonoBehaviour
{
    [Header("视角配置")]
    public GameObject fpcRoot;
    public Transform fpcPlayer;
    public GameObject tpcRoot;
    public Transform tpcPlayer;

    private StarterAssetsInputs fpcInput, tpcInput;
    private MonoBehaviour fpcScript, tpcScript;
    private Animator tpcAnimator; // 新增：缓存Animator

    void Awake()
    {
        InitializeComponents();
        if (fpcRoot) fpcRoot.SetActive(false);
        if (tpcRoot) tpcRoot.SetActive(false);
    }

    void Start()
    {
        if (GameData.Instance == null) return;

        // 确保初始化时也重置一下状态
        ResetCharacterState(fpcRoot);
        ResetCharacterState(tpcRoot);

        if (GameData.Instance.ShouldRestorePosition)
        {
            SetViewMode(GameData.Instance.WasFirstPerson, true);
            GameData.Instance.ShouldRestorePosition = false;
        }
        else
        {
            // 默认优先第一人称，除非面板设置了偏好
            bool defaultIsFps = true;
            if (SettingPanel.Instance != null && SettingPanel.Instance.defaultViewToggle != null)
                defaultIsFps = SettingPanel.Instance.defaultViewToggle.isOn;

            SetViewMode(defaultIsFps, false);
        }
    }

    void Update()
    {
        KeyCode key = SettingPanel.KeyConfig.ViewSwitchKey;
        if (Input.GetKeyDown(key)) SetViewMode(!IsInFirstPerson(), false);
    }

    public void SetViewMode(bool toFps, bool isRestoring)
    {
        if (fpcRoot == null || tpcRoot == null) return;

        GameObject targetRoot = toFps ? fpcRoot : tpcRoot;
        Transform targetPlayer = toFps ? fpcPlayer : tpcPlayer;
        GameObject oldRoot = toFps ? tpcRoot : fpcRoot;
        Transform oldPlayer = toFps ? tpcPlayer : fpcPlayer;

        // 1. 记录状态并关闭旧对象
        if (oldRoot.activeSelf)
        {
            // 在关闭前，也可以尝试清空旧对象的输入，防止后台跑
            ResetInput(toFps ? tpcInput : fpcInput);
        }
        oldRoot.SetActive(false);

        // 2. 同步位置
        if (isRestoring)
        {
            CharacterController cc = targetPlayer.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            targetPlayer.position = GameData.Instance.LastPlayerPosition;
            targetPlayer.rotation = GameData.Instance.LastPlayerRotation;
            if (cc) cc.enabled = true;
        }
        else
        {
            if (oldPlayer != null)
            {
                CharacterController cc = targetPlayer.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                targetPlayer.position = oldPlayer.position;

                // 第一人称转第三人称时，保留朝向；反之亦然
                // 只有当两个角色模型朝向逻辑一致时才直接赋值
                targetPlayer.rotation = oldPlayer.rotation;

                if (cc) cc.enabled = true;
            }
        }

        // 3. 激活新对象
        targetRoot.SetActive(true);

        // 【关键修复】激活后立刻重置输入和动画状态，防止“鬼畜”自动行走
        StarterAssetsInputs targetInput = toFps ? fpcInput : tpcInput;
        ResetInput(targetInput);
        ResetCharacterState(targetRoot);

        UpdateCharacterStats(toFps ? fpcScript : tpcScript);

        // 更新 GameData 记录
        if (GameData.Instance) GameData.Instance.WasFirstPerson = toFps;
    }

    // 辅助方法：重置输入
    private void ResetInput(StarterAssetsInputs input)
    {
        if (input != null)
        {
            input.move = Vector2.zero;
            input.look = Vector2.zero;
            input.jump = false;
            input.sprint = false;
        }
    }

    // 辅助方法：重置动画机和刚体速度
    private void ResetCharacterState(GameObject root)
    {
        if (root == null) return;

        // 重置 Animator 参数
        Animator anim = root.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("MotionSpeed", 1f);
            // 如果有其他的 Bool 参数比如 "IsWalking"，也要设为 false
        }

        // 也可以选择性重置 CharacterController 的动量（虽然 CC 没有直接的速度属性，但可以通过逻辑脚本重置）
    }

    public bool IsInFirstPerson() { return fpcRoot != null && fpcRoot.activeSelf; }
    public Transform GetActivePlayerTransform() { return IsInFirstPerson() ? fpcPlayer : tpcPlayer; }

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
            tpcAnimator = tpcRoot.GetComponentInChildren<Animator>(true);
        }
    }

    private void UpdateCharacterStats(MonoBehaviour script)
    {
        if (script == null || GameData.Instance == null) return;

        float speed = GameData.Instance.MoveSpeed;
        float jump = GameData.Instance.JumpHeight;

        SetPublicField(script, "MoveSpeed", speed);
        SetPublicField(script, "SprintSpeed", speed * 1.5f);
        SetPublicField(script, "JumpHeight", jump);
    }

    private void SetPublicField(object target, string name, float val)
    {
        if (target == null) return;
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null) field.SetValue(target, val);
    }
}