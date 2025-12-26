using UnityEngine;
using StarterAssets;
using System.Reflection;
using System;

public class SwitchViews : MonoBehaviour
{
    [Header("视角配置")]
    public GameObject fpcRoot;
    public Transform fpcPlayer;
    public GameObject tpcRoot;
    public Transform tpcPlayer;

    private StarterAssetsInputs fpcInput, tpcInput;
    private MonoBehaviour fpcScript, tpcScript;

    void Awake() { InitializeComponents(); if (fpcRoot) fpcRoot.SetActive(false); if (tpcRoot) tpcRoot.SetActive(false); }

    void Start()
    {
        if (GameData.Instance == null) return;
        if (GameData.Instance.ShouldRestorePosition)
        {
            SetViewMode(GameData.Instance.WasFirstPerson, true);
            GameData.Instance.ShouldRestorePosition = false;
        }
        else
        {
            bool defaultIsFps = SettingPanel.Instance != null ? SettingPanel.Instance.defaultViewToggle.isOn : true;
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

        oldRoot.SetActive(false);

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
                targetPlayer.rotation = oldPlayer.rotation;
                if (cc) cc.enabled = true;
            }
        }
        targetRoot.SetActive(true);
        UpdateCharacterStats(toFps ? fpcScript : tpcScript);
    }

    public bool IsInFirstPerson() { return fpcRoot != null && fpcRoot.activeSelf; }
    public Transform GetActivePlayerTransform() { return IsInFirstPerson() ? fpcPlayer : tpcPlayer; }

    private void InitializeComponents()
    {
        if (fpcRoot) { fpcInput = fpcRoot.GetComponentInChildren<StarterAssetsInputs>(true); fpcScript = fpcRoot.GetComponentInChildren<FirstPersonController>(true); }
        if (tpcRoot) { tpcInput = tpcRoot.GetComponentInChildren<StarterAssetsInputs>(true); tpcScript = tpcRoot.GetComponentInChildren<ThirdPersonController>(true); }
    }

    private void UpdateCharacterStats(MonoBehaviour script)
    {
        if (script == null || GameData.Instance == null) return;

        // 【直接读取 GameData，告别 0 值噩梦】
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