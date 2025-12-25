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

    //事件委托：当设置更改时通知其他脚本
    public delegate void OnSettingChangedDelegate(SettingDate newSettings);
    public static event OnSettingChangedDelegate OnSettingsChanged;

    //委托定义：应用设置的函数类型
    public delegate void ApplySettingDelegate(SettingDate settings);

    //注册表：存储其他脚本需要应用设置的方法
    private static List<ApplySettingDelegate> applyDelegates = new List<ApplySettingDelegate>();

    // 公开这个状态，让 PlayerInteraction 等脚本可以读取，防止误触
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
    public TMP_Dropdown viewKeyDropdown;    // 视角切换键
    public TMP_Dropdown callPanelDropdown;  // 呼出面板键
    public Slider mouseXSlider;             // 鼠标 X 灵敏度
    public Slider mouseYSlider;             // 鼠标 Y 灵敏度

    [Header("=== 🚶 漫游设置 UI ===")]
    public Toggle defaultViewToggle;            // 是否默认第一人称
    public TMP_InputField moveSpeedInput;       // 移动速度
    public TMP_InputField jumpHeightInput;      // 跳跃高度
    public TMP_InputField interactionDistInput; // 交互距离
    public Slider footstepVolumeSlider;         // 脚步音量
    public TMP_InputField stepDistInput;        // 步长

    [Header("=== 🔊 音效与系统 UI ===")]
    public Slider bgmVolumeSlider;          // 背景音乐
    public Slider videoVolumeSlider;        // 视频音量
    public Slider descriptionVolumeSlider;  // 解说音量
    public Slider buttonVolumeSlider;       // 按钮音量
    public TMP_InputField loadingTimeInput; // 加载最小等待时间
    public TMP_InputField loopCountInput;   // 视频循环次数

    [Header("=== 🔘 底部按钮 ===")]
    public Button saveButton;       // 保存设置
    public Button exitButton;       // 退出体验

    [Header("=== ⚙️ 场景配置 ===")]
    [Tooltip("开始场景的名字 (在此场景点击退出 -> 关闭游戏)")]
    public string startSceneName = "StartGame";
    [Tooltip("浏览馆主场景的名字 (在此场景点击退出 -> 回开始界面)")]
    public string mainSceneName = "Museum_Main";
    [Tooltip("加载场景的名字 (在此场景无法呼出面板)")]
    public string loadingSceneName = "LoadingScene";

    // ==========================================================
    // 3. 数据类定义
    // ==========================================================
    [System.Serializable]
    public class SettingDate
    {
        // 控制
        public KeyCode viewSwitchKey = KeyCode.T;
        public KeyCode callSettingPanelKey = KeyCode.Escape;
        public float mouseXSensitivity = 1.0f;
        public float mouseYSensitivity = 1.0f;

        // 漫游
        public bool defaultFirstPersonView = true;
        public float moveSpeed = 5f;
        public float jumpHeight = 3f;
        public float interactionDistance = 10f;
        public float footstepVolume = 0.5f;
        public float stepDistance = 1.8f;

        // 音效与系统
        public float bgmVolume = 1f;
        public float videoVolume = 1f;
        public float descriptionVolume = 1f;
        public float buttonVolume = 1f;
        public float loadingTime = 5f;
        public int startGameVideoLoopCount = 2;
    }
    public SettingDate settingData = new SettingDate();


    //公共属性：让其他脚本可以直接读取当前设置（只读）
    public static SettingDate CurrentSettings
    {
        get { return Instance != null ? Instance.settingData : new SettingDate(); }
    }


    // 预定义的按键列表 (用于 Dropdown)
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
        // 每次换场景，先强制关闭面板并恢复时间，防止卡死
        if (isPanelActive)
        {
            SwitchSettingPanel(false);
        }
        // 【新增】场景加载后自动应用设置到新场景中的对象
        Invoke("NotifySettingsChanged", 0.1f);
    }

    private void Start()
    {
        // 确保面板UI一开始是隐藏的
        if (panelRoot != null) panelRoot.SetActive(false);
        isPanelActive = false;

        LoadSettings();       // 读取存档
        InitUIValues();       // 刷新UI显示
        BindUIEvents();       // 绑定事件
        NotifySettingsChanged(); // 【修改】改为通知所有注册对象
    }

    private void Update()
    {
        // 1. 如果在加载界面，禁止任何操作
        if (SceneManager.GetActiveScene().name == loadingSceneName) return;

        // 2. 始终检测 ESC 按键
        if (Input.GetKeyDown(settingData.callSettingPanelKey))
        {
            SwitchSettingPanel(!isPanelActive); // 切换开关状态
        }
    }

    // ==========================================================
    // 5. 核心接口：供其他脚本注册和获取设置
    // ==========================================================

    /// <summary>
    /// 注册一个应用设置的回调函数
    /// 其他脚本可以在 Awake 或 Start 中调用此方法来注册
    /// </summary>
    public static void RegisterApplyMethod(ApplySettingDelegate applyMethod)
    {
        if (!applyDelegates.Contains(applyMethod))
        {
            applyDelegates.Add(applyMethod);

            // 立即应用当前设置到新注册的对象
            if (Instance != null)
            {
                applyMethod(Instance.settingData);
            }
        }
    }

    /// <summary>
    /// 取消注册应用设置的回调函数
    /// 脚本在 OnDestroy 时应该调用此方法
    /// </summary>
    public static void UnregisterApplyMethod(ApplySettingDelegate applyMethod)
    {
        if (applyDelegates.Contains(applyMethod))
        {
            applyDelegates.Remove(applyMethod);
        }
    }

    /// <summary>
    /// 手动触发设置应用（供UI事件调用）
    /// </summary>
    private void NotifySettingsChanged()
    {
        // 触发事件
        if (OnSettingsChanged != null)
        {
            OnSettingsChanged(settingData);
        }

        // 调用所有注册的委托
        foreach (var applyMethod in applyDelegates.ToList()) // 使用副本防止修改时出错
        {
            try
            {
                applyMethod(settingData);
            }
            catch (Exception e)
            {
                Debug.LogError($"应用设置时出错: {e.Message}");
            }
        }

        // 兼容旧代码：调用原有的 ApplySettingsToGame
        ApplySettingsToGame();
    }

    /// <summary>
    /// 强制刷新所有设置（供外部调用）
    /// </summary>
    public void ForceRefreshSettings()
    {
        NotifySettingsChanged();
    }

    // ==========================================================
    // 6. 面板开关与暂停核心逻辑
    // ==========================================================
    public void SwitchSettingPanel(bool isOpen)
    {
        if (panelRoot == null) return;

        isPanelActive = isOpen;
        panelRoot.SetActive(isPanelActive);

        if (isPanelActive)
        {
            // === 打开面板时 ===
            // 1. 暂停游戏时间 (停止物理、移动、动画)
            Time.timeScale = 0f;

            // 2. 解锁并显示鼠标 (确保能点 UI)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // === 关闭面板时 ===
            // 1. 恢复游戏时间
            Time.timeScale = 1f;

            // 2. 根据场景决定是否锁定鼠标
            string currentScene = SceneManager.GetActiveScene().name;

            // 如果在开始界面，鼠标应该始终可见
            if (currentScene == startSceneName)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 如果在游戏里 (博物馆/展品)，关闭面板后隐藏鼠标
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // 重载版本，方便按钮调用（不传参默认切换）
    public void SwitchSettingPanel()
    {
        SwitchSettingPanel(!isPanelActive);
    }

    // ==========================================================
    // 7. 智能退出逻辑 (Exit Button)
    // ==========================================================
    public void OnExitButton()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 1. 如果是 Loading，不准动
        if (currentScene == loadingSceneName) return;

        Debug.Log("正在执行退出逻辑，当前场景：" + currentScene);

        // 恢复时间 (否则跳转场景后游戏还是暂停的)
        Time.timeScale = 1f;
        // 关闭面板状态记录
        isPanelActive = false;
        if (panelRoot) panelRoot.SetActive(false);

        // 2. 如果在开始界面 -> 退出游戏
        if (currentScene == startSceneName)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
        // 3. 如果在浏览馆主界面 -> 返回开始界面
        else if (currentScene == mainSceneName)
        {
            // 使用加载器跳转
            if (System.Type.GetType("SceneLoding") != null)
                SceneLoding.LoadLevel(startSceneName);
            else
                SceneManager.LoadScene(startSceneName);
        }
        // 4. 如果在其他展品界面 (图片/视频/全景) -> 返回浏览馆
        else
        {
            if (System.Type.GetType("SceneLoding") != null)
                SceneLoding.LoadLevel(mainSceneName);
            else
                SceneManager.LoadScene(mainSceneName);
        }
    }

    // ==========================================================
    // 8. 初始化 UI 显示 (InitUIValues)
    // ==========================================================
    private void InitUIValues()
    {
        // Dropdown
        UpdateDropdownSelection(viewKeyDropdown, settingData.viewSwitchKey);
        UpdateDropdownSelection(callPanelDropdown, settingData.callSettingPanelKey);

        // Sliders
        if (mouseXSlider) mouseXSlider.value = settingData.mouseXSensitivity;
        if (mouseYSlider) mouseYSlider.value = settingData.mouseYSensitivity;
        if (footstepVolumeSlider) footstepVolumeSlider.value = settingData.footstepVolume;
        if (bgmVolumeSlider) bgmVolumeSlider.value = settingData.bgmVolume;
        if (videoVolumeSlider) videoVolumeSlider.value = settingData.videoVolume;
        if (descriptionVolumeSlider) descriptionVolumeSlider.value = settingData.descriptionVolume;
        if (buttonVolumeSlider) buttonVolumeSlider.value = settingData.buttonVolume;

        // InputFields & Toggle
        if (defaultViewToggle) defaultViewToggle.isOn = settingData.defaultFirstPersonView;
        if (moveSpeedInput) moveSpeedInput.text = settingData.moveSpeed.ToString();
        if (jumpHeightInput) jumpHeightInput.text = settingData.jumpHeight.ToString();
        if (interactionDistInput) interactionDistInput.text = settingData.interactionDistance.ToString();
        if (stepDistInput) stepDistInput.text = settingData.stepDistance.ToString();
        if (loadingTimeInput) loadingTimeInput.text = settingData.loadingTime.ToString();
        if (loopCountInput) loopCountInput.text = settingData.startGameVideoLoopCount.ToString();
    }

    // ==========================================================
    // 9. 绑定 UI 事件 (BindUIEvents)
    // ==========================================================
    private void BindUIEvents()
    {
        // Slider 事件
        if (mouseXSlider) mouseXSlider.onValueChanged.AddListener((v) => {
            settingData.mouseXSensitivity = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (mouseYSlider) mouseYSlider.onValueChanged.AddListener((v) => {
            settingData.mouseYSensitivity = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (footstepVolumeSlider) footstepVolumeSlider.onValueChanged.AddListener((v) => {
            settingData.footstepVolume = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });

        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener((v) => {
            settingData.bgmVolume = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.AddListener((v) => {
            settingData.videoVolume = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.AddListener((v) => {
            settingData.descriptionVolume = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.AddListener((v) => {
            settingData.buttonVolume = v;
            NotifySettingsChanged(); // 【修改】替换为通知
        });

        // Toggle 事件
        if (defaultViewToggle) defaultViewToggle.onValueChanged.AddListener((isOn) => {
            settingData.defaultFirstPersonView = isOn;
            NotifySettingsChanged(); // 【修改】替换为通知
        });

        // InputField 事件 (使用 onEndEdit 避免频繁调用)
        if (moveSpeedInput) moveSpeedInput.onEndEdit.AddListener((str) => {
            if (float.TryParse(str, out float v))
            {
                settingData.moveSpeed = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });
        if (jumpHeightInput) jumpHeightInput.onEndEdit.AddListener((str) => {
            if (float.TryParse(str, out float v))
            {
                settingData.jumpHeight = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });
        if (interactionDistInput) interactionDistInput.onEndEdit.AddListener((str) => {
            if (float.TryParse(str, out float v))
            {
                settingData.interactionDistance = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });
        if (stepDistInput) stepDistInput.onEndEdit.AddListener((str) => {
            if (float.TryParse(str, out float v))
            {
                settingData.stepDistance = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });

        if (loadingTimeInput) loadingTimeInput.onEndEdit.AddListener((str) => {
            if (float.TryParse(str, out float v))
            {
                settingData.loadingTime = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });
        if (loopCountInput) loopCountInput.onEndEdit.AddListener((str) => {
            if (int.TryParse(str, out int v))
            {
                settingData.startGameVideoLoopCount = v;
                NotifySettingsChanged(); // 【修改】替换为通知
            }
        });

        // Dropdown 事件
        if (viewKeyDropdown) viewKeyDropdown.onValueChanged.AddListener((idx) => {
            settingData.viewSwitchKey = dropdownKeys[idx];
            NotifySettingsChanged(); // 【修改】替换为通知
        });
        if (callPanelDropdown) callPanelDropdown.onValueChanged.AddListener((idx) => {
            settingData.callSettingPanelKey = dropdownKeys[idx];
            NotifySettingsChanged(); // 【修改】替换为通知
        });

        // 按钮事件
        // 1. 保存设置
        if (saveButton)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => {
                SaveSettings();
                NotifySettingsChanged(); // 【修改】替换为通知
            });
        }

        // 3. 退出体验 (绑定到 OnExitButton)
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButton);
        }
    }

    // ==========================================================
    // 10. 应用数据到游戏 (ApplySettingsToGame) - 保留原有逻辑
    // ==========================================================
    public void ApplySettingsToGame()
    {
        // 1. 同步 SwitchViews (负责角色移动、跳跃、视角)
        SwitchViews switchViews = FindObjectOfType<SwitchViews>();
        if (switchViews != null)
        {
            switchViews.switchKey = settingData.viewSwitchKey;
            switchViews.startInFirstPerson = settingData.defaultFirstPersonView;

            switchViews.UpdateCharacterSettings(
                settingData.moveSpeed,
                settingData.jumpHeight,
                settingData.mouseXSensitivity
            );
        }

        // 2. 同步 PlayerInteraction (负责交互距离)
        // 使用 true 参数查找隐藏物体，确保无遗漏
        PlayerInteraction[] interactions = FindObjectsOfType<PlayerInteraction>(true);
        foreach (var interaction in interactions)
        {
            interaction.interactionDistance = settingData.interactionDistance;
        }

        // 3. 同步 FirstPersonFootAudios (负责脚步声)
        FirstPersonFootAudios[] footAudios = FindObjectsOfType<FirstPersonFootAudios>(true);
        foreach (var audio in footAudios)
        {
            audio.volume = settingData.footstepVolume;
            audio.stepDistance = settingData.stepDistance;
        }

        // 4. 同步加载时间 (查找 SceneLoding)
        SceneLoding loader = FindObjectOfType<SceneLoding>();
        if (loader != null)
        {
            loader.minLoadTime = settingData.loadingTime;
        }

        // 5. 同步开始游戏视频循环
        StartGame startGame = FindObjectOfType<StartGame>();
        if (startGame != null)
        {
            startGame.loopTimesWithSound = settingData.startGameVideoLoopCount;
        }

        // 6. 全局音量 (可选)
        // AudioListener.volume = settingData.bgmVolume;
    }

    // 辅助：更新 Dropdown 选中项
    private void UpdateDropdownSelection(TMP_Dropdown dropdown, KeyCode currentKey)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(dropdownKeys.Select(k => k.ToString()).ToList());
        int index = dropdownKeys.IndexOf(currentKey);
        if (index >= 0) dropdown.value = index;
    }

    // ==========================================================
    // 11. 存档系统 (Save & Load)
    // ==========================================================
    public void SaveSettings()
    {
        PlayerPrefs.SetString("ViewSwitchKey", settingData.viewSwitchKey.ToString());
        PlayerPrefs.SetString("CallSettingPanelKey", settingData.callSettingPanelKey.ToString());

        PlayerPrefs.SetFloat("MouseX", settingData.mouseXSensitivity);
        PlayerPrefs.SetFloat("MouseY", settingData.mouseYSensitivity);
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
        if (PlayerPrefs.HasKey("ViewSwitchKey")) Enum.TryParse(PlayerPrefs.GetString("ViewSwitchKey"), out settingData.viewSwitchKey);
        if (PlayerPrefs.HasKey("CallSettingPanelKey")) Enum.TryParse(PlayerPrefs.GetString("CallSettingPanelKey"), out settingData.callSettingPanelKey);

        if (PlayerPrefs.HasKey("MouseX")) settingData.mouseXSensitivity = PlayerPrefs.GetFloat("MouseX");
        if (PlayerPrefs.HasKey("MouseY")) settingData.mouseYSensitivity = PlayerPrefs.GetFloat("MouseY");
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