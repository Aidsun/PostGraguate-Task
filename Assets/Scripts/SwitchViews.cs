// 文件：SwitchViews.cs
// 模块：玩家控制器 / 视角切换
// 说明：该脚本负责管理第一人称（FPC）和第三人称（TPC）视角的切换。
//      它维护两个角色根对象，通过激活/禁用它们实现视角切换，并同步位置和旋转。
//      支持从GameData恢复保存的视角状态（如从展示场景返回主馆时）。
//      使用StarterAssets输入系统，切换时重置输入状态防止角色自动移动。
//      还负责从GameData更新角色移动速度和跳跃高度等参数。
// 特性：依赖StarterAssets包，使用反射设置私有字段，通过GameData和SettingPanel获取配置。

using UnityEngine;
using StarterAssets;          // 使用StarterAssets的输入和控制器
using System.Reflection;       // 使用反射访问私有字段

public class SwitchViews : MonoBehaviour
{
    [Header("视角配置")]               // Inspector分组
    public GameObject fpcRoot;          // 第一人称角色的根对象（包含相机、控制器等）
    public Transform fpcPlayer;         // 第一人称玩家的Transform（通常用于位置同步）
    public GameObject tpcRoot;          // 第三人称角色的根对象
    public Transform tpcPlayer;         // 第三人称玩家的Transform

    // 私有字段：缓存输入组件和控制器脚本，提高性能
    private StarterAssetsInputs fpcInput, tpcInput;          // 输入处理
    private MonoBehaviour fpcScript, tpcScript;               // 具体的控制器脚本（FirstPersonController / ThirdPersonController）
    private Animator tpcAnimator;                             // 第三人称的动画器（用于重置状态）

    void Awake()
    {
        // 初始化组件引用
        InitializeComponents();
        // 初始时两个角色都禁用，由Start根据条件激活其中一个
        if (fpcRoot) fpcRoot.SetActive(false);
        if (tpcRoot) tpcRoot.SetActive(false);
    }

    void Start()
    {
        if (GameData.Instance == null) return;

        // 确保初始化时重置一下动画状态，防止残留动画
        ResetCharacterState(fpcRoot);
        ResetCharacterState(tpcRoot);

        // 如果GameData指示需要恢复玩家位置（例如从展示场景返回）
        if (GameData.Instance.ShouldRestorePosition)
        {
            // 切换到之前记录的视角模式（第一人称或第三人称），并传入true表示恢复位置
            SetViewMode(GameData.Instance.WasFirstPerson, true);
            GameData.Instance.ShouldRestorePosition = false; // 清除标记
        }
        else
        {
            // 默认优先第一人称，除非设置面板中defaultViewToggle指定了其他偏好
            bool defaultIsFps = true;
            if (SettingPanel.Instance != null && SettingPanel.Instance.defaultViewToggle != null)
                defaultIsFps = SettingPanel.Instance.defaultViewToggle.isOn; // true为第一人称

            SetViewMode(defaultIsFps, false); // 不恢复位置
        }
    }

    void Update()
    {
        // 从SettingPanel获取配置的视角切换按键
        KeyCode key = SettingPanel.KeyConfig.ViewSwitchKey;
        if (Input.GetKeyDown(key))
        {
            // 切换视角到相反模式
            SetViewMode(!IsInFirstPerson(), false);
        }
    }

    /// <summary>
    /// 设置视角模式。
    /// </summary>
    /// <param name="toFps">true表示切换到第一人称，false表示切换到第三人称</param>
    /// <param name="isRestoring">是否为恢复位置模式（如果是，则从GameData.LastPlayerPosition读取位置）</param>
    public void SetViewMode(bool toFps, bool isRestoring)
    {
        if (fpcRoot == null || tpcRoot == null) return;

        // 确定目标角色根和玩家Transform，以及旧角色根
        GameObject targetRoot = toFps ? fpcRoot : tpcRoot;
        Transform targetPlayer = toFps ? fpcPlayer : tpcPlayer;
        GameObject oldRoot = toFps ? tpcRoot : fpcRoot;
        Transform oldPlayer = toFps ? tpcPlayer : fpcPlayer;

        // 1. 记录状态并关闭旧对象
        if (oldRoot.activeSelf)
        {
            // 在关闭前，重置旧对象的输入，防止后台仍然接收输入
            ResetInput(toFps ? tpcInput : fpcInput);
        }
        oldRoot.SetActive(false); // 禁用旧角色

        // 2. 同步位置
        if (isRestoring)
        {
            // 恢复模式：从GameData读取保存的位置和旋转
            CharacterController cc = targetPlayer.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;   // 暂时禁用CharacterController以便直接设置位置
            targetPlayer.position = GameData.Instance.LastPlayerPosition;
            targetPlayer.rotation = GameData.Instance.LastPlayerRotation;
            if (cc) cc.enabled = true;
        }
        else
        {
            // 非恢复模式：将新角色的位置和旋转设为旧角色的位置和旋转
            if (oldPlayer != null)
            {
                CharacterController cc = targetPlayer.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                targetPlayer.position = oldPlayer.position;
                // 注意：第一人称和第三人称的朝向逻辑可能不同，但这里简单保持相同旋转
                targetPlayer.rotation = oldPlayer.rotation;
                if (cc) cc.enabled = true;
            }
        }

        // 3. 激活新对象
        targetRoot.SetActive(true);

        // 【关键修复】激活后立刻重置输入和动画状态，防止角色自动行走（鬼畜）
        StarterAssetsInputs targetInput = toFps ? fpcInput : tpcInput;
        ResetInput(targetInput);
        ResetCharacterState(targetRoot);

        // 将GameData中的移动速度、跳跃高度等参数应用到当前角色的控制器脚本
        UpdateCharacterStats(toFps ? fpcScript : tpcScript);

        // 更新GameData中记录的当前视角模式
        if (GameData.Instance) GameData.Instance.WasFirstPerson = toFps;
    }

    // 辅助方法：重置输入组件的值，避免残留输入导致角色移动
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

    // 辅助方法：重置动画状态，防止自动行走动画
    private void ResetCharacterState(GameObject root)
    {
        if (root == null) return;

        // 重置Animator参数
        Animator anim = root.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("MotionSpeed", 1f);
            // 如果有其他的Bool参数如"IsWalking"，也可在此设置，但需要知道具体参数名
        }
    }

    /// <summary>
    /// 判断当前是否处于第一人称视角。
    /// </summary>
    public bool IsInFirstPerson() { return fpcRoot != null && fpcRoot.activeSelf; }

    /// <summary>
    /// 获取当前激活的玩家角色的Transform。
    /// </summary>
    public Transform GetActivePlayerTransform() { return IsInFirstPerson() ? fpcPlayer : tpcPlayer; }

    /// <summary>
    /// 初始化组件引用，从两个角色根下查找需要的脚本。
    /// </summary>
    private void InitializeComponents()
    {
        if (fpcRoot)
        {
            fpcInput = fpcRoot.GetComponentInChildren<StarterAssetsInputs>(true); // 包括未激活的子物体
            fpcScript = fpcRoot.GetComponentInChildren<FirstPersonController>(true);
        }
        if (tpcRoot)
        {
            tpcInput = tpcRoot.GetComponentInChildren<StarterAssetsInputs>(true);
            tpcScript = tpcRoot.GetComponentInChildren<ThirdPersonController>(true);
            tpcAnimator = tpcRoot.GetComponentInChildren<Animator>(true); // 缓存备用
        }
    }

    /// <summary>
    /// 从GameData读取移动速度和跳跃高度，并更新到指定控制器脚本的对应字段。
    /// 使用反射设置公共字段，因为控制器脚本可能不是我们写的，无法直接访问内部字段。
    /// </summary>
    private void UpdateCharacterStats(MonoBehaviour script)
    {
        if (script == null || GameData.Instance == null) return;

        float speed = GameData.Instance.MoveSpeed;
        float jump = GameData.Instance.JumpHeight;

        // 设置移动速度、冲刺速度、跳跃高度
        SetPublicField(script, "MoveSpeed", speed);
        SetPublicField(script, "SprintSpeed", speed * 1.5f); // 默认冲刺速度为移动速度的1.5倍
        SetPublicField(script, "JumpHeight", jump);
    }

    /// <summary>
    /// 通过反射设置对象的公共实例字段。
    /// </summary>
    private void SetPublicField(object target, string name, float val)
    {
        if (target == null) return;
        // 获取公共实例字段
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null) field.SetValue(target, val);
    }
}