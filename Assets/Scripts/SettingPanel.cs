using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public class SettingPanel : MonoBehaviour
{
    // ==========================================================
    // 1. 单例与基础变量
    // ==========================================================
    public static SettingPanel Instance;

    public delegate void OnSettingChangedDelegate(SettingDate newSettings);
    public static event OnSettingChangedDelegate OnSettingsChanged;

    public delegate void ApplySettingDelegate(SettingDate settings);
    private static List<ApplySettingDelegate> applyDelegates = new List<ApplySettingDelegate>();

    [HideInInspector]
    public bool isPanelActive = false;

    [Header("【核心组件】")]
    [Tooltip("UI面板的根节点 (包含所有内容的父物体)")]
    public GameObject panelRoot;

    // ==========================================================
    // 2. UI 绑定区域
    // ==========================================================
    [Space(10)]
    [Header("=== 🎮 控制设置 UI ===")]
    public TMP_Dropdown viewKeyDropdown;
    public TMP_Dropdown callPanelDropdown;

    [Header("=== 🚶 漫游设置 UI ===")]
    public Toggle defaultViewToggle;
    public TMP_InputField moveSpeedInput;
    public TMP_InputField jumpHeightInput;
    public TMP_InputField interactionDistInput;
    public Slider footstepVolumeSlider;
    public TMP_InputField stepDistInput;

    [Header("=== 🔊 音效与系统 UI ===")]
    public Slider bgmVolumeSlider;
    public Slider videoVolumeSlider;
    public Slider descriptionVolumeSlider;
    public Slider buttonVolumeSlider;
    public TMP_InputField loadingTimeInput;
    public TMP_InputField loopCountInput;

    [Header("=== 🔘 底部按钮 ===")]
    public Button saveButton;
    public Button exitButton;

    [Header("=== ⚙️ 场景配置 ===")]
    public string startSceneName = "StartGame";
    public string mainSceneName = "Museum_Main";
    public string loadingSceneName = "LoadingScene";

    // ==========================================================
    // 3. 数据类定义
    // ==========================================================
    [System.Serializable]
    public class SettingDate
    {
        public KeyCode viewSwitchKey = KeyCode.T;

        // 【核心修改】默认值设为 Tab
        public KeyCode callSettingPanelKey = KeyCode.Tab;

        [HideInInspector] public float mouseXSensitivity = 1.5f;
        [HideInInspector] public float mouseYSensitivity = 1.5f;

        public bool defaultFirstPersonView = true;
        public float moveSpeed = 5f;
        public float jumpHeight = 3f;
        public float interactionDistance = 10f;
        public float footstepVolume = 0.5f;
        public float stepDistance = 1.8f;

        public float bgmVolume = 1f;
        public float videoVolume = 1f;
        public float descriptionVolume = 1f;
        public float buttonVolume = 1f;
        public float loadingTime = 5f;
        public int startGameVideoLoopCount = 2;
    }
    public SettingDate settingData = new SettingDate();

    public static SettingDate CurrentSettings
    {
        get { return Instance != null ? Instance.settingData : new SettingDate(); }
    }

    private readonly List<KeyCode> dropdownKeys = new List<KeyCode>()
    {
        KeyCode.T, KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.Tab,
        KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.LeftShift, KeyCode.LeftAlt
    };

    // ==========================================================
    // 4. 生命周期逻辑
    // ==========================================================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPanelActive)
        {
            SwitchSettingPanel(false);
        }
        Invoke("NotifySettingsChanged", 0.1f);
    }

    private void Start()
    {
        // 1. 设置层级，防止点击穿透
        SetupPanelLayer();

        if (panelRoot != null) panelRoot.SetActive(false);
        isPanelActive = false;

        // 2. 读取设置
        LoadSettings();
        InitUIValues();
        BindUIEvents();
        NotifySettingsChanged();
    }

    private void SetupPanelLayer()
    {
        if (panelRoot == null) return;

        // 强制设置 Canvas 层级为最高 (999)，覆盖所有其他 UI
        Canvas cv = panelRoot.GetComponent<Canvas>();
        if (cv == null) cv = panelRoot.AddComponent<Canvas>();

        cv.overrideSorting = true;
        cv.sortingOrder = 999;

        // 确保能接收点击
        if (panelRoot.GetComponent<GraphicRaycaster>() == null)
            panelRoot.AddComponent<GraphicRaycaster>();

        // 确保能拦截点击
        if (panelRoot.GetComponent<CanvasGroup>() == null)
            panelRoot.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == loadingSceneName) return;

        if (Input.GetKeyDown(settingData.callSettingPanelKey))
        {
            SwitchSettingPanel(!isPanelActive);
        }
    }

    // ==========================================================
    // 5. 核心接口
    // ==========================================================
    public static void RegisterApplyMethod(ApplySettingDelegate applyMethod)
    {
        if (!applyDelegates.Contains(applyMethod))
        {
            applyDelegates.Add(applyMethod);
            if (Instance != null) applyMethod(Instance.settingData);
        }
    }

    public static void UnregisterApplyMethod(ApplySettingDelegate applyMethod)
    {
        if (applyDelegates.Contains(applyMethod)) applyDelegates.Remove(applyMethod);
    }

    private void NotifySettingsChanged()
    {
        if (OnSettingsChanged != null) OnSettingsChanged(settingData);

        foreach (var applyMethod in applyDelegates.ToList())
        {
            try { applyMethod(settingData); }
            catch (Exception e) { Debug.LogError($"应用设置出错: {e.Message}"); }
        }
        ApplySettingsToGame();
    }

    // ==========================================================
    // 6. 面板开关
    // ==========================================================
    public void SwitchSettingPanel(bool isOpen)
    {
        if (panelRoot == null) return;

        isPanelActive = isOpen;
        panelRoot.SetActive(isPanelActive);

        // 控制射线拦截
        CanvasGroup cg = panelRoot.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = isPanelActive;

        if (isPanelActive)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == startSceneName)
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

    public void SwitchSettingPanel()
    {
        SwitchSettingPanel(!isPanelActive);
    }

    // ==========================================================
    // 7. 退出逻辑
    // ==========================================================
    public void OnExitButton()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == loadingSceneName) return;

        Time.timeScale = 1f;
        isPanelActive = false;
        if (panelRoot) panelRoot.SetActive(false);

        if (currentScene == startSceneName)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
        else if (currentScene == mainSceneName)
        {
            GameDate.ShouldRestorePosition = false;
            LoadSceneSafe(startSceneName);
        }
        else
        {
            GameDate.ShouldRestorePosition = true;
            LoadSceneSafe(mainSceneName);
        }
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // ==========================================================
    // 8. 初始化与绑定
    // ==========================================================
    private void InitUIValues()
    {
        UpdateDropdownSelection(viewKeyDropdown, settingData.viewSwitchKey);
        UpdateDropdownSelection(callPanelDropdown, settingData.callSettingPanelKey);

        if (footstepVolumeSlider) footstepVolumeSlider.value = settingData.footstepVolume;
        if (bgmVolumeSlider) bgmVolumeSlider.value = settingData.bgmVolume;
        if (videoVolumeSlider) videoVolumeSlider.value = settingData.videoVolume;
        if (descriptionVolumeSlider) descriptionVolumeSlider.value = settingData.descriptionVolume;
        if (buttonVolumeSlider) buttonVolumeSlider.value = settingData.buttonVolume;

        if (defaultViewToggle) defaultViewToggle.isOn = settingData.defaultFirstPersonView;
        if (moveSpeedInput) moveSpeedInput.text = settingData.moveSpeed.ToString();
        if (jumpHeightInput) jumpHeightInput.text = settingData.jumpHeight.ToString();
        if (interactionDistInput) interactionDistInput.text = settingData.interactionDistance.ToString();
        if (stepDistInput) stepDistInput.text = settingData.stepDistance.ToString();
        if (loadingTimeInput) loadingTimeInput.text = settingData.loadingTime.ToString();
        if (loopCountInput) loopCountInput.text = settingData.startGameVideoLoopCount.ToString();
    }

    private void BindUIEvents()
    {
        if (footstepVolumeSlider) footstepVolumeSlider.onValueChanged.AddListener((v) => { settingData.footstepVolume = v; NotifySettingsChanged(); });
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener((v) => { settingData.bgmVolume = v; NotifySettingsChanged(); });
        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.AddListener((v) => { settingData.videoVolume = v; NotifySettingsChanged(); });
        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.AddListener((v) => { settingData.descriptionVolume = v; NotifySettingsChanged(); });
        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.AddListener((v) => { settingData.buttonVolume = v; NotifySettingsChanged(); });

        if (defaultViewToggle) defaultViewToggle.onValueChanged.AddListener((isOn) => { settingData.defaultFirstPersonView = isOn; NotifySettingsChanged(); });

        if (moveSpeedInput) moveSpeedInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) { settingData.moveSpeed = v; NotifySettingsChanged(); } });
        if (jumpHeightInput) jumpHeightInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) { settingData.jumpHeight = v; NotifySettingsChanged(); } });
        if (interactionDistInput) interactionDistInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) { settingData.interactionDistance = v; NotifySettingsChanged(); } });
        if (stepDistInput) stepDistInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) { settingData.stepDistance = v; NotifySettingsChanged(); } });
        if (loadingTimeInput) loadingTimeInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) { settingData.loadingTime = v; NotifySettingsChanged(); } });
        if (loopCountInput) loopCountInput.onEndEdit.AddListener((str) => { if (int.TryParse(str, out int v)) { settingData.startGameVideoLoopCount = v; NotifySettingsChanged(); } });

        if (viewKeyDropdown) viewKeyDropdown.onValueChanged.AddListener((idx) => { settingData.viewSwitchKey = dropdownKeys[idx]; NotifySettingsChanged(); });
        if (callPanelDropdown) callPanelDropdown.onValueChanged.AddListener((idx) => { settingData.callSettingPanelKey = dropdownKeys[idx]; NotifySettingsChanged(); });

        if (saveButton) { saveButton.onClick.RemoveAllListeners(); saveButton.onClick.AddListener(() => { SaveSettings(); NotifySettingsChanged(); }); }
        if (exitButton) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(OnExitButton); }
    }

    // ==========================================================
    // 9. 应用与辅助
    // ==========================================================
    public void ApplySettingsToGame()
    {
        SwitchViews switchViews = FindObjectOfType<SwitchViews>();
        if (switchViews != null)
        {
            switchViews.switchKey = settingData.viewSwitchKey;
            switchViews.startInFirstPerson = settingData.defaultFirstPersonView;
            switchViews.UpdateCharacterSettings(settingData.moveSpeed, settingData.jumpHeight, settingData.mouseXSensitivity);
        }
        PlayerInteraction[] interactions = FindObjectsOfType<PlayerInteraction>(true);
        foreach (var interaction in interactions) interaction.interactionDistance = settingData.interactionDistance;

        FirstPersonFootAudios[] footAudios = FindObjectsOfType<FirstPersonFootAudios>(true);
        foreach (var audio in footAudios) { audio.volume = settingData.footstepVolume; audio.stepDistance = settingData.stepDistance; }

        SceneLoding loader = FindObjectOfType<SceneLoding>();
        if (loader != null) loader.minLoadTime = settingData.loadingTime;

        StartGame startGame = FindObjectOfType<StartGame>();
        if (startGame != null) startGame.loopTimesWithSound = settingData.startGameVideoLoopCount;
    }

    private void UpdateDropdownSelection(TMP_Dropdown dropdown, KeyCode currentKey)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(dropdownKeys.Select(k => k.ToString()).ToList());
        int index = dropdownKeys.IndexOf(currentKey);
        if (index >= 0) dropdown.value = index;
    }

    // ==========================================================
    // 10. 存档系统
    // ==========================================================
    public void SaveSettings()
    {
        // 【关键修复】使用 _V2 新键名保存，强制刷新旧存档
        PlayerPrefs.SetString("ViewSwitchKey_V2", settingData.viewSwitchKey.ToString());
        PlayerPrefs.SetString("CallPanelKey_V2", settingData.callSettingPanelKey.ToString()); // 强制使用新Key

        PlayerPrefs.SetInt("DefaultView", settingData.defaultFirstPersonView ? 1 : 0);
        PlayerPrefs.SetFloat("MoveSpeed", settingData.moveSpeed);
        PlayerPrefs.SetFloat("JumpHeight", settingData.jumpHeight);
        PlayerPrefs.SetFloat("InteractDist", settingData.interactionDistance);
        PlayerPrefs.SetFloat("FootVol", settingData.footstepVolume);
        PlayerPrefs.SetFloat("StepDist", settingData.stepDistance);
        PlayerPrefs.SetFloat("BGMVol", settingData.bgmVolume);
        PlayerPrefs.SetFloat("VideoVol", settingData.videoVolume);
        PlayerPrefs.SetFloat("DescVol", settingData.descriptionVolume);
        PlayerPrefs.SetFloat("BtnVol", settingData.buttonVolume);
        PlayerPrefs.SetFloat("LoadingTime", settingData.loadingTime);
        PlayerPrefs.SetInt("LoopCount", settingData.startGameVideoLoopCount);
        PlayerPrefs.Save();
        Debug.Log("设置已保存！");
    }

    private void LoadSettings()
    {
        // 【关键修复】只读取 _V2 新键名
        // 如果存档里没有 _V2 (第一次运行新代码)，它就会使用代码最上面定义的 KeyCode.Tab
        if (PlayerPrefs.HasKey("ViewSwitchKey_V2")) Enum.TryParse(PlayerPrefs.GetString("ViewSwitchKey_V2"), out settingData.viewSwitchKey);
        if (PlayerPrefs.HasKey("CallPanelKey_V2")) Enum.TryParse(PlayerPrefs.GetString("CallPanelKey_V2"), out settingData.callSettingPanelKey);

        if (PlayerPrefs.HasKey("DefaultView")) settingData.defaultFirstPersonView = PlayerPrefs.GetInt("DefaultView") == 1;
        if (PlayerPrefs.HasKey("MoveSpeed")) settingData.moveSpeed = PlayerPrefs.GetFloat("MoveSpeed");
        if (PlayerPrefs.HasKey("JumpHeight")) settingData.jumpHeight = PlayerPrefs.GetFloat("JumpHeight");
        if (PlayerPrefs.HasKey("InteractDist")) settingData.interactionDistance = PlayerPrefs.GetFloat("InteractDist");
        if (PlayerPrefs.HasKey("FootVol")) settingData.footstepVolume = PlayerPrefs.GetFloat("FootVol");
        if (PlayerPrefs.HasKey("StepDist")) settingData.stepDistance = PlayerPrefs.GetFloat("StepDist");
        if (PlayerPrefs.HasKey("BGMVol")) settingData.bgmVolume = PlayerPrefs.GetFloat("BGMVol");
        if (PlayerPrefs.HasKey("VideoVol")) settingData.videoVolume = PlayerPrefs.GetFloat("VideoVol");
        if (PlayerPrefs.HasKey("DescVol")) settingData.descriptionVolume = PlayerPrefs.GetFloat("DescVol");
        if (PlayerPrefs.HasKey("BtnVol")) settingData.buttonVolume = PlayerPrefs.GetFloat("BtnVol");
        if (PlayerPrefs.HasKey("LoadingTime")) settingData.loadingTime = PlayerPrefs.GetFloat("LoadingTime");
        if (PlayerPrefs.HasKey("LoopCount")) settingData.startGameVideoLoopCount = PlayerPrefs.GetInt("LoopCount");
    }
}