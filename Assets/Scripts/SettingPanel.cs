using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Video; // 【重要】引入视频控制命名空间

public class SettingPanel : MonoBehaviour
{
    public static SettingPanel Instance;

    [Header("【核心组件】")]
    public GameObject panelRoot;

    [Space(10)]
    [Header("=== 🔊 音量滑块绑定 (修改GameData) ===")]
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

    // 【新增】用于播放面板音效的音源
    private AudioSource uiAudioSource;

    [System.Serializable]
    public class InputConfig
    {
        public KeyCode ViewSwitchKey = KeyCode.T;
        public KeyCode CallPanelKey = KeyCode.Tab;
    }
    public static InputConfig KeyConfig = new InputConfig();

    private readonly List<KeyCode> dropdownKeys = new List<KeyCode>() {
        KeyCode.T, KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.Tab,
        KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.LeftShift, KeyCode.LeftAlt
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem_AutoCreated");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("🔧 [SettingPanel] 已自动修复缺失的 EventSystem");
        }
        InitUI();
        BindEvents();
    }

    private void Start()
    {
        SetupPanelLayer();
        if (panelRoot != null) panelRoot.SetActive(false);
        isPanelActive = false;

        // 【新增】初始化音频组件
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
        }
        // 设置为2D声音，防止因为位置听不见
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.playOnAwake = false;

        // 确保游戏开始时时间是正常的
        Time.timeScale = 1f;

        InitUI();
        BindEvents();
    }

    private void InitUI()
    {
        if (GameData.Instance != null)
        {
            if (bgmVolumeSlider) bgmVolumeSlider.SetValueWithoutNotify(GameData.Instance.BgmVolume);
            if (videoVolumeSlider) videoVolumeSlider.SetValueWithoutNotify(GameData.Instance.VideoVolume);
            if (descriptionVolumeSlider) descriptionVolumeSlider.SetValueWithoutNotify(GameData.Instance.VoiceVolume);
            if (buttonVolumeSlider) buttonVolumeSlider.SetValueWithoutNotify(GameData.Instance.ButtonVolume);

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
            var helper = bgmVolumeSlider.GetComponentInChildren<UI_SliderValue>();
            if (helper) helper.UpdateText(v);
        });

        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.VideoVolume = v;
            var helper = videoVolumeSlider.GetComponentInChildren<UI_SliderValue>();
            if (helper) helper.UpdateText(v);
        });

        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.VoiceVolume = v;
            var helper = descriptionVolumeSlider.GetComponentInChildren<UI_SliderValue>();
            if (helper) helper.UpdateText(v);
        });

        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.ButtonVolume = v;
            var helper = buttonVolumeSlider.GetComponentInChildren<UI_SliderValue>();
            if (helper) helper.UpdateText(v);
        });

        BindInput(moveSpeedInput, (v) => GameData.Instance.MoveSpeed = v);
        BindInput(jumpHeightInput, (v) => GameData.Instance.JumpHeight = v);
        BindInput(interactionDistInput, (v) => GameData.Instance.InteractionDistance = v);
        BindInput(stepDistInput, (v) => GameData.Instance.StepDistance = v);

        if (viewKeyDropdown) { viewKeyDropdown.onValueChanged.RemoveAllListeners(); viewKeyDropdown.onValueChanged.AddListener((idx) => KeyConfig.ViewSwitchKey = dropdownKeys[idx]); }
        if (callPanelDropdown) { callPanelDropdown.onValueChanged.RemoveAllListeners(); callPanelDropdown.onValueChanged.AddListener((idx) => KeyConfig.CallPanelKey = dropdownKeys[idx]); }
        if (videoControlDropdown) { videoControlDropdown.onValueChanged.RemoveAllListeners(); videoControlDropdown.onValueChanged.AddListener((idx) => { if (GameData.Instance) GameData.Instance.VideoPauseKey = dropdownKeys[idx]; }); }

        if (saveButton) { saveButton.onClick.RemoveAllListeners(); saveButton.onClick.AddListener(SaveSettings); }
        if (exitButton) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(OnExitButton); }
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

    // 【核心修改】
    public void SwitchSettingPanel(bool isOpen)
    {
        isPanelActive = isOpen;
        if (panelRoot) panelRoot.SetActive(isOpen);

        if (isOpen)
        {
            // 1. 暂停游戏逻辑
            Time.timeScale = 0f;

            // 【修复】使用 InternalTime 让视频跟随游戏时间暂停
            VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();
            foreach (var vp in allVideoPlayers)
            {
                if (vp != null) vp.timeReference = VideoTimeReference.InternalTime; // 这里改成了 InternalTime
            }

            // 2. 播放打开音效
            if (GameData.Instance && GameData.Instance.PanelOpenSound)
            {
                if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource != null)
                {
                    uiAudioSource.PlayOneShot(GameData.Instance.PanelOpenSound, GameData.Instance.ButtonVolume);
                }
            }

            // 3. 解锁鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 1. 恢复游戏逻辑
            Time.timeScale = 1f;

            // 2. 鼠标状态恢复
            if (SceneManager.GetActiveScene().name == "StartGame")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void OnExitButton()
    {
        Time.timeScale = 1f; // 退出前必须恢复时间

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
        else if (currentScene == "Museum_Main") { SceneManager.LoadScene("StartGame"); }
        else { if (GameData.Instance) GameData.Instance.ShouldRestorePosition = true; SceneLoading.LoadLevel("Museum_Main"); }
    }

    private void SaveSettings()
    {
        Debug.Log("设置已保存");
        SwitchSettingPanel(false);
    }
}