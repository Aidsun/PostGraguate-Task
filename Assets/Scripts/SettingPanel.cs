using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Video;

public class SettingPanel : MonoBehaviour
{
    public static SettingPanel Instance;

    [Header("【核心组件】")]
    public GameObject panelRoot;

    [Space(10)]
    [Header("=== 🔊 音量滑块绑定 ===")]
    public Slider bgmVolumeSlider;
    public Slider videoVolumeSlider;
    public Slider descriptionVolumeSlider;
    public Slider buttonVolumeSlider;

    [Header("=== 🎮 其他设置 UI ===")]
    public TMP_Dropdown viewKeyDropdown;
    public TMP_Dropdown callPanelDropdown;
    public TMP_Dropdown videoControlDropdown;
    public Toggle defaultViewToggle;
    public TMP_InputField moveSpeedInput;
    public TMP_InputField jumpHeightInput;
    public TMP_InputField interactionDistInput;
    public TMP_InputField stepDistInput;
    public Button saveButton;
    public Button exitButton;

    [HideInInspector] public bool isPanelActive = false;
    private AudioSource uiAudioSource;

    [System.Serializable]
    public class InputConfig { public KeyCode ViewSwitchKey = KeyCode.T; public KeyCode CallPanelKey = KeyCode.Tab; }
    public static InputConfig KeyConfig = new InputConfig();

    private readonly List<KeyCode> dropdownKeys = new List<KeyCode>() { KeyCode.T, KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.Tab, KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.LeftShift, KeyCode.LeftAlt };

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem_AutoCreated");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        if (scene.name == "StartGame") { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; Time.timeScale = 1f; }
        else if (scene.name == "Museum_Main") { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; Time.timeScale = 1f; }

        isPanelActive = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        InitUI();
        BindEvents();
    }

    private void Start()
    {
        SetupPanelLayer();
        if (panelRoot != null) panelRoot.SetActive(false);
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void InitUI()
    {
        if (GameData.Instance != null)
        {
            if (bgmVolumeSlider) bgmVolumeSlider.SetValueWithoutNotify(GameData.Instance.BgmVolume);
            if (videoVolumeSlider) videoVolumeSlider.SetValueWithoutNotify(GameData.Instance.VideoVolume);
            if (descriptionVolumeSlider) descriptionVolumeSlider.SetValueWithoutNotify(GameData.Instance.VoiceVolume);
            if (buttonVolumeSlider) buttonVolumeSlider.SetValueWithoutNotify(GameData.Instance.ButtonVolume);

            UpdateSliderText(bgmVolumeSlider, GameData.Instance.BgmVolume);
            UpdateSliderText(videoVolumeSlider, GameData.Instance.VideoVolume);
            UpdateSliderText(descriptionVolumeSlider, GameData.Instance.VoiceVolume);
            UpdateSliderText(buttonVolumeSlider, GameData.Instance.ButtonVolume);

            if (moveSpeedInput) moveSpeedInput.text = GameData.Instance.MoveSpeed.ToString();
            if (jumpHeightInput) jumpHeightInput.text = GameData.Instance.JumpHeight.ToString();
            if (interactionDistInput) interactionDistInput.text = GameData.Instance.InteractionDistance.ToString();
            if (stepDistInput) stepDistInput.text = GameData.Instance.StepDistance.ToString();

            UpdateDropdownSelection(videoControlDropdown, GameData.Instance.VideoPauseKey);
        }
        UpdateDropdownSelection(viewKeyDropdown, KeyConfig.ViewSwitchKey);
        UpdateDropdownSelection(callPanelDropdown, KeyConfig.CallPanelKey);
    }

    private void BindEvents()
    {
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.RemoveAllListeners();
        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.RemoveAllListeners();
        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.RemoveAllListeners();

        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.BgmVolume = v;
            if (AudioManager.Instance) AudioManager.Instance.UpdateMixerVolume();
            UpdateSliderText(bgmVolumeSlider, v);
        });

        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.VideoVolume = v;
            if (AudioManager.Instance) AudioManager.Instance.UpdateMixerVolume();
            UpdateSliderText(videoVolumeSlider, v);
        });

        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.VoiceVolume = v;
            if (AudioManager.Instance) AudioManager.Instance.UpdateMixerVolume();
            UpdateSliderText(descriptionVolumeSlider, v);
        });

        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.ButtonVolume = v;
            if (AudioManager.Instance) AudioManager.Instance.UpdateMixerVolume();
            UpdateSliderText(buttonVolumeSlider, v);
        });

        BindInput(moveSpeedInput, (v) => GameData.Instance.MoveSpeed = v);
        BindInput(jumpHeightInput, (v) => GameData.Instance.JumpHeight = v);
        BindInput(interactionDistInput, (v) => GameData.Instance.InteractionDistance = v);
        BindInput(stepDistInput, (v) => GameData.Instance.StepDistance = v);

        if (viewKeyDropdown) { viewKeyDropdown.onValueChanged.RemoveAllListeners(); viewKeyDropdown.onValueChanged.AddListener((idx) => KeyConfig.ViewSwitchKey = dropdownKeys[idx]); }
        if (callPanelDropdown) { callPanelDropdown.onValueChanged.RemoveAllListeners(); callPanelDropdown.onValueChanged.AddListener((idx) => KeyConfig.CallPanelKey = dropdownKeys[idx]); }
        if (videoControlDropdown) { videoControlDropdown.onValueChanged.RemoveAllListeners(); videoControlDropdown.onValueChanged.AddListener((idx) => { if (GameData.Instance) GameData.Instance.VideoPauseKey = dropdownKeys[idx]; }); }

        if (saveButton) { saveButton.onClick.RemoveAllListeners(); saveButton.onClick.AddListener(() => { PlayButtonSound(); SaveSettings(); }); }
        if (exitButton) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(() => { PlayButtonSound(); OnExitButton(); }); }
    }

    void UpdateSliderText(Slider s, float val)
    {
        if (s == null) return;
        var helper = s.GetComponent<UI_SliderValue>();
        if (helper == null) helper = s.GetComponentInChildren<UI_SliderValue>();
        if (helper) helper.UpdateText(val);
    }

    void BindInput(TMP_InputField input, System.Action<float> onValChange)
    {
        if (input == null) return;
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v) && GameData.Instance) onValChange(v); });
    }

    private void UpdateDropdownSelection(TMP_Dropdown dropdown, KeyCode currentKey) { if (dropdown == null) return; dropdown.ClearOptions(); dropdown.AddOptions(dropdownKeys.Select(k => k.ToString()).ToList()); int index = dropdownKeys.IndexOf(currentKey); if (index >= 0) dropdown.value = index; }

    private void SetupPanelLayer()
    {
        if (panelRoot == null) return;
        Canvas cv = panelRoot.GetComponent<Canvas>();
        if (cv == null) cv = panelRoot.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 9999;
        if (panelRoot.GetComponent<GraphicRaycaster>() == null) panelRoot.AddComponent<GraphicRaycaster>();
        if (panelRoot.GetComponent<CanvasGroup>() == null) panelRoot.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "LoadingScene") return;
        KeyCode callKey = KeyConfig.CallPanelKey == KeyCode.None ? KeyCode.Tab : KeyConfig.CallPanelKey;
        if (Input.GetKeyDown(callKey)) SwitchSettingPanel(!isPanelActive);
    }

    // =========================================================
    // 【核心修复】关闭面板时，检查是否有其他UI正开着
    // =========================================================
    public void SwitchSettingPanel(bool isOpen)
    {
        isPanelActive = isOpen;
        if (panelRoot) panelRoot.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();
            foreach (var vp in allVideoPlayers) { if (vp != null) vp.timeReference = VideoTimeReference.InternalTime; }

            if (GameData.Instance && GameData.Instance.PanelOpenSound)
            {
                if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource != null) uiAudioSource.PlayOneShot(GameData.Instance.PanelOpenSound, GameData.Instance.ButtonVolume);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;

            // 1. 先检查 TutorPanel 是否开着
            bool isTutorOpen = false;
            if (TutorPanel.Instance && TutorPanel.Instance.panelObject.activeSelf) isTutorOpen = true;

            // 2. 检查 答题面板 是否开着 (QuestionManager)
            bool isQuestionOpen = false;
            if (QuestionManager.Instance && QuestionManager.Instance.QuestionPanel && QuestionManager.Instance.QuestionPanel.activeSelf) isQuestionOpen = true;

            // 只有当：是开始界面 OR 提示面板开着 OR 答题面板开着，才保留鼠标
            if (SceneManager.GetActiveScene().name == "StartGame" || isTutorOpen || isQuestionOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 否则才锁鼠标
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void OnExitButton()
    {
        Time.timeScale = 1f;
        SwitchSettingPanel(false);
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "StartGame")
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else if (currentScene == "Museum_Main")
        {
            SceneManager.LoadScene("StartGame");
        }
        else
        {
            if (GameData.Instance)
            {
                if (GameData.Instance.TempSafeState.HasData)
                {
                    var safeData = GameData.Instance.TempSafeState;
                    GameData.Instance.LastPlayerPosition = safeData.Position;
                    GameData.Instance.LastPlayerRotation = safeData.Rotation;
                    GameData.Instance.WasFirstPerson = safeData.IsFirstPerson;
                    GameData.Instance.ShouldRestorePosition = true;
                    GameData.Instance.TempSafeState.HasData = false;
                }
                else { GameData.Instance.ShouldRestorePosition = false; }
            }
            SceneLoading.LoadLevel("Museum_Main");
        }
    }

    private void SaveSettings() { Debug.Log("设置已保存"); SwitchSettingPanel(false); }
    private void PlayButtonSound() { if (GameData.Instance && GameData.Instance.ButtonClickSound) { if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>(); if (uiAudioSource != null) uiAudioSource.PlayOneShot(GameData.Instance.ButtonClickSound, GameData.Instance.ButtonVolume); } }
}