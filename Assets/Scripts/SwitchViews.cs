using UnityEngine;
using StarterAssets; // 关键：引用 Starter Assets 的命名空间

public class SwitchViews : MonoBehaviour
{
    [Header("First Person Setup (第一人称设置)")]
    [Tooltip("第一人称角色父节点")]
    public GameObject fpcRoot;
    [Tooltip("第一人称角色模型")]
    public Transform fpcPlayer; // 第一人称胶囊体

    [Header("Third Person Setup (第三人称设置)")]
    [Tooltip("第三人称角色父节点")]
    public GameObject tpcRoot;
    [Tooltip("第三人称角色模型")]
    public Transform tpcPlayer; // 第三人称 Armature

    [Header("视角切换快捷键")]
    public KeyCode switchKey = KeyCode.T;
    //第一人称视角记录器
    private bool isFirstPerson = false;
    [Header("是否默认启用第一视角")]
    public bool startInFirstPerson = false;

    // 缓存输入组件，用于重置状态
    private StarterAssetsInputs fpcInput;
    private StarterAssetsInputs tpcInput;

    void Start()
    {
        // 自动获取输入组件
        if (fpcRoot) fpcInput = fpcRoot.GetComponentInChildren<StarterAssetsInputs>();
        if (tpcRoot) tpcInput = tpcRoot.GetComponentInChildren<StarterAssetsInputs>();

        // 1. 设置内部状态
        isFirstPerson = startInFirstPerson;

        // 2. 根据您的设置，正确初始化显隐状态
        if (startInFirstPerson)
        {
            // 如果勾选了默认第一人称：
            // 同步位置：假设您在场景里摆放的是第三人称模型的位置，先把第一人称挪过去
            SyncTransform(tpcPlayer, fpcPlayer);

            if (fpcRoot) fpcRoot.SetActive(true);  // 开第一人称
            if (tpcRoot) tpcRoot.SetActive(false); // 关第三人称

            // 此时不用重置输入，因为游戏刚开始还没输入
        }
        else
        {
            // 如果没勾选（默认第三人称）：
            // 同步位置：以防万一
            SyncTransform(fpcPlayer, tpcPlayer);

            if (fpcRoot) fpcRoot.SetActive(false); // 关第一人称
            if (tpcRoot) tpcRoot.SetActive(true);  // 开第三人称
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleView();
        }
    }

    private void ToggleView()
    {
        if (isFirstPerson)
        {
            // 切换到第三人称
            SwitchToThirdPerson();
        }
        else
        {
            // 切换到第一人称
            SwitchToFirstPerson();
        }

        isFirstPerson = !isFirstPerson;
    }

    private void SwitchToFirstPerson()
    {
        // 1. 同步位置 (TPC -> FPC)
        SyncTransform(tpcPlayer, fpcPlayer);

        // 2.【关键修复】禁用 TPC 前，先重置它的输入，防止它下次醒来自动走路
        if (tpcInput != null) ResetInput(tpcInput);

        // 3. 切换物体激活状态
        tpcRoot.SetActive(false);
        fpcRoot.SetActive(true);
    }

    private void SwitchToThirdPerson()
    {
        // 1. 同步位置 (FPC -> TPC)
        SyncTransform(fpcPlayer, tpcPlayer);

        // 2.【关键修复】禁用 FPC 前，先重置它的输入
        if (fpcInput != null) ResetInput(fpcInput);

        // 3. 切换物体激活状态
        fpcRoot.SetActive(false);
        tpcRoot.SetActive(true);
    }

    // 核心修复逻辑：强制将输入归零
    private void ResetInput(StarterAssetsInputs input)
    {
        input.move = Vector2.zero;   // 停止移动
        input.look = Vector2.zero;   // 停止旋转视角
        input.jump = false;          // 取消跳跃预输入
        input.sprint = false;        // 取消冲刺
    }

    private void SyncTransform(Transform source, Transform target)
    {
        CharacterController cc = target.GetComponent<CharacterController>();

        bool wasEnabled = false;
        if (cc != null)
        {
            wasEnabled = cc.enabled;
            cc.enabled = false;
        }

        target.position = source.position;
        target.rotation = source.rotation;

        if (cc != null && wasEnabled)
        {
            cc.enabled = true;
        }
    }
}