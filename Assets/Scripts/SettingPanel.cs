// 文件：SettingPanel.cs
// 模块：UI / 设置面板
// 说明：该脚本是全局设置面板的管理器，负责控制设置面板的显示/隐藏、音量调节、按键绑定、
//      游戏参数设置（移动速度、跳跃高度等）、保存设置、退出游戏等功能。
//      它监听场景加载，自动创建EventSystem，并根据当前场景调整鼠标状态。
//      设置面板的打开/关闭由玩家按指定按键触发（默认为Tab），打开时会暂停游戏时间、解锁鼠标，
//      关闭时根据其他UI（TutorPanel、QuestionPanel）状态决定是否锁定鼠标。
// 特性：单例模式，DontDestroyOnLoad跨场景持久化，监听SceneManager.sceneLoaded事件，
//      使用Dropdown、Slider、Toggle、InputField等多种UI组件，通过GameData读写数据，
//      与AudioManager交互更新音量，与SceneLoading交互进行场景切换。

using UnityEngine;
using UnityEngine.SceneManagement;          // 场景管理
using UnityEngine.UI;                       // UI组件（Slider, Button, Toggle等）
using UnityEngine.EventSystems;              // 事件系统
using TMPro;                                 // TextMeshPro组件
using System.Collections.Generic;            // 泛型集合
using System.Linq;                           // LINQ扩展（用于列表操作）
using UnityEngine.Video;                     // VideoPlayer组件（用于处理视频暂停）

public class SettingPanel : MonoBehaviour
{
    // 单例实例
    public static SettingPanel Instance;

    [Header("【核心组件】")]
    public GameObject panelRoot;              // 设置面板的根物体

    [Space(10)]
    [Header("=== 🔊 音量滑块绑定 ===")]
    public Slider bgmVolumeSlider;             // 背景音乐音量滑块
    public Slider videoVolumeSlider;           // 视频音量滑块
    public Slider descriptionVolumeSlider;     // 解说音量滑块
    public Slider buttonVolumeSlider;          // 按钮音效音量滑块

    [Header("=== 🎮 其他设置 UI ===")]
    public TMP_Dropdown viewKeyDropdown;       // 视角切换按键下拉菜单
    public TMP_Dropdown callPanelDropdown;     // 呼出设置面板按键下拉菜单
    public TMP_Dropdown videoControlDropdown;  // 视频控制按键下拉菜单
    public Toggle defaultViewToggle;            // 默认视角切换开关（第一人称/第三人称）
    public TMP_InputField moveSpeedInput;       // 移动速度输入框
    public TMP_InputField jumpHeightInput;      // 跳跃高度输入框
    public TMP_InputField interactionDistInput; // 交互距离输入框
    public TMP_InputField stepDistInput;        // 步长距离输入框（脚步声触发间隔）
    public Button saveButton;                    // 保存设置按钮
    public Button exitButton;                    // 退出按钮

    [HideInInspector] public bool isPanelActive = false;  // 标记面板是否处于打开状态
    private AudioSource uiAudioSource;                      // 用于播放UI音效的音频源

    // 按键配置类，可序列化，用于存储视角切换键和呼出面板键
    [System.Serializable]
    public class InputConfig { public KeyCode ViewSwitchKey = KeyCode.T; public KeyCode CallPanelKey = KeyCode.Tab; }
    public static InputConfig KeyConfig = new InputConfig();   // 静态配置，供其他脚本读取

    // 下拉菜单中可选的按键列表
    private readonly List<KeyCode> dropdownKeys = new List<KeyCode>() {
        KeyCode.T, KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.Tab,
        KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.LeftShift, KeyCode.LeftAlt
    };

    private void Awake()
    {
        // 标准单例实现：如果不存在则设置为当前实例并跨场景保持，否则销毁
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // 当脚本启用时，订阅场景加载事件
    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }

    // 当脚本禁用时，取消订阅场景加载事件
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    /// <summary>
    /// 场景加载完成后调用，用于初始化UI、绑定事件、调整鼠标状态等。
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果场景中不存在EventSystem，则自动创建一个，确保UI输入正常工作
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem_AutoCreated");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();  // 添加标准输入模块
        }

        // 根据场景名称设置初始鼠标状态和时间缩放
        if (scene.name == "StartGame")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
        }
        else if (scene.name == "Museum_Main")
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }

        // 确保面板初始为关闭状态
        isPanelActive = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        // 初始化UI控件显示值
        InitUI();
        // 绑定事件监听
        BindEvents();
    }

    private void Start()
    {
        // 设置面板的渲染层级，确保它显示在其他UI之上
        SetupPanelLayer();
        if (panelRoot != null) panelRoot.SetActive(false);

        // 获取或添加AudioSource用于播放UI音效
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null) uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;

        // 手动调用一次OnSceneLoaded，确保当前场景的初始化（因为Awake和OnEnable在场景加载后可能已执行过）
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    /// <summary>
    /// 初始化UI控件的显示值，从GameData读取数据并设置到控件上。
    /// </summary>
    private void InitUI()
    {
        if (GameData.Instance != null)
        {
            // 设置音量滑块的值（不触发事件）
            if (bgmVolumeSlider) bgmVolumeSlider.SetValueWithoutNotify(GameData.Instance.BgmVolume);
            if (videoVolumeSlider) videoVolumeSlider.SetValueWithoutNotify(GameData.Instance.VideoVolume);
            if (descriptionVolumeSlider) descriptionVolumeSlider.SetValueWithoutNotify(GameData.Instance.VoiceVolume);
            if (buttonVolumeSlider) buttonVolumeSlider.SetValueWithoutNotify(GameData.Instance.ButtonVolume);

            // 更新滑块旁边显示的数值文本（通过UI_SliderValue辅助脚本）
            UpdateSliderText(bgmVolumeSlider, GameData.Instance.BgmVolume);
            UpdateSliderText(videoVolumeSlider, GameData.Instance.VideoVolume);
            UpdateSliderText(descriptionVolumeSlider, GameData.Instance.VoiceVolume);
            UpdateSliderText(buttonVolumeSlider, GameData.Instance.ButtonVolume);

            // 设置输入框文本
            if (moveSpeedInput) moveSpeedInput.text = GameData.Instance.MoveSpeed.ToString();
            if (jumpHeightInput) jumpHeightInput.text = GameData.Instance.JumpHeight.ToString();
            if (interactionDistInput) interactionDistInput.text = GameData.Instance.InteractionDistance.ToString();
            if (stepDistInput) stepDistInput.text = GameData.Instance.StepDistance.ToString();

            // 更新视频控制按键下拉菜单的选中项
            UpdateDropdownSelection(videoControlDropdown, GameData.Instance.VideoPauseKey);
        }
        // 更新视角切换按键和呼出面板按键的下拉菜单
        UpdateDropdownSelection(viewKeyDropdown, KeyConfig.ViewSwitchKey);
        UpdateDropdownSelection(callPanelDropdown, KeyConfig.CallPanelKey);
    }

    /// <summary>
    /// 绑定所有UI控件的事件监听。
    /// </summary>
    private void BindEvents()
    {
        // 先移除所有可能存在的旧监听，防止重复绑定
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.RemoveAllListeners();
        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.RemoveAllListeners();
        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.RemoveAllListeners();
        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.RemoveAllListeners();

        // 为音量滑块添加值改变监听
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener((v) => {
            if (GameData.Instance) GameData.Instance.BgmVolume = v;
            if (AudioManager.Instance) AudioManager.Instance.UpdateMixerVolume(); // 更新音频混合器
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

        // 绑定输入框的结束编辑事件
        BindInput(moveSpeedInput, (v) => GameData.Instance.MoveSpeed = v);
        BindInput(jumpHeightInput, (v) => GameData.Instance.JumpHeight = v);
        BindInput(interactionDistInput, (v) => GameData.Instance.InteractionDistance = v);
        BindInput(stepDistInput, (v) => GameData.Instance.StepDistance = v);

        // 绑定下拉菜单的选择改变事件
        if (viewKeyDropdown)
        {
            viewKeyDropdown.onValueChanged.RemoveAllListeners();
            viewKeyDropdown.onValueChanged.AddListener((idx) => KeyConfig.ViewSwitchKey = dropdownKeys[idx]);
        }
        if (callPanelDropdown)
        {
            callPanelDropdown.onValueChanged.RemoveAllListeners();
            callPanelDropdown.onValueChanged.AddListener((idx) => KeyConfig.CallPanelKey = dropdownKeys[idx]);
        }
        if (videoControlDropdown)
        {
            videoControlDropdown.onValueChanged.RemoveAllListeners();
            videoControlDropdown.onValueChanged.AddListener((idx) => { if (GameData.Instance) GameData.Instance.VideoPauseKey = dropdownKeys[idx]; });
        }

        // 绑定按钮点击事件
        if (saveButton)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(() => { PlayButtonSound(); SaveSettings(); });
        }
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => { PlayButtonSound(); OnExitButton(); });
        }
    }

    /// <summary>
    /// 更新滑块旁边的数值文本（通过子物体上的UI_SliderValue脚本）。
    /// </summary>
    void UpdateSliderText(Slider s, float val)
    {
        if (s == null) return;
        var helper = s.GetComponent<UI_SliderValue>();
        if (helper == null) helper = s.GetComponentInChildren<UI_SliderValue>();
        if (helper) helper.UpdateText(val);
    }

    /// <summary>
    /// 为输入框绑定结束编辑事件，并将字符串解析为float后赋值给GameData的对应字段。
    /// </summary>
    void BindInput(TMP_InputField input, System.Action<float> onValChange)
    {
        if (input == null) return;
        input.onEndEdit.RemoveAllListeners();
        input.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v) && GameData.Instance) onValChange(v); });
    }

    /// <summary>
    /// 更新下拉菜单的选中项，使其与当前按键配置一致。
    /// </summary>
    private void UpdateDropdownSelection(TMP_Dropdown dropdown, KeyCode currentKey)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        // 将按键列表转换为字符串列表作为下拉选项
        dropdown.AddOptions(dropdownKeys.Select(k => k.ToString()).ToList());
        int index = dropdownKeys.IndexOf(currentKey);
        if (index >= 0) dropdown.value = index;
    }

    /// <summary>
    /// 设置面板的Canvas属性，确保它显示在最上层。
    /// </summary>
    private void SetupPanelLayer()
    {
        if (panelRoot == null) return;
        Canvas cv = panelRoot.GetComponent<Canvas>();
        if (cv == null) cv = panelRoot.AddComponent<Canvas>();
        cv.overrideSorting = true;          // 覆盖排序
        cv.sortingOrder = 9999;             // 设置极高的渲染顺序
        if (panelRoot.GetComponent<GraphicRaycaster>() == null) panelRoot.AddComponent<GraphicRaycaster>(); // 添加射线检测
        if (panelRoot.GetComponent<CanvasGroup>() == null) panelRoot.AddComponent<CanvasGroup>(); // 用于控制透明度等（虽未使用）
    }

    private void Update()
    {
        // 如果当前场景是LoadingScene，则不处理按键（避免冲突）
        if (SceneManager.GetActiveScene().name == "LoadingScene") return;
        KeyCode callKey = KeyConfig.CallPanelKey == KeyCode.None ? KeyCode.Tab : KeyConfig.CallPanelKey;
        // 检测呼出面板的按键，切换面板状态
        if (Input.GetKeyDown(callKey)) SwitchSettingPanel(!isPanelActive);
    }

    // =========================================================
    // 【核心修复】关闭面板时，检查是否有其他UI正开着
    // =========================================================
    /// <summary>
    /// 切换设置面板的打开/关闭状态。
    /// 打开时暂停游戏、解锁鼠标、暂停所有视频播放；
    /// 关闭时恢复游戏时间，并根据其他UI状态决定是否锁定鼠标。
    /// </summary>
    public void SwitchSettingPanel(bool isOpen)
    {
        isPanelActive = isOpen;
        if (panelRoot) panelRoot.SetActive(isOpen);

        if (isOpen)
        {
            // 打开面板时暂停游戏时间
            Time.timeScale = 0f;
            // 查找所有VideoPlayer并设置其时间参考为内部时间，避免暂停时出现问题（可能是在编辑器中的处理）
            VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();
            foreach (var vp in allVideoPlayers) { if (vp != null) vp.timeReference = VideoTimeReference.InternalTime; }

            // 播放面板打开音效
            if (GameData.Instance && GameData.Instance.PanelOpenSound)
            {
                if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
                if (uiAudioSource != null) uiAudioSource.PlayOneShot(GameData.Instance.PanelOpenSound, GameData.Instance.ButtonVolume);
            }
            // 解锁并显示鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 关闭面板时恢复游戏时间
            Time.timeScale = 1f;

            // 1. 先检查 TutorPanel 是否开着
            bool isTutorOpen = false;
            if (TutorPanel.Instance && TutorPanel.Instance.panelObject.activeSelf) isTutorOpen = true;

            // 2. 检查 答题面板 是否开着 (QuestionManager)
            bool isQuestionOpen = false;
            if (QuestionManager.Instance && QuestionManager.Instance.QuestionPanel && QuestionManager.Instance.QuestionPanel.activeSelf) isQuestionOpen = true;

            // 只有当：是开始界面 OR 提示面板开着 OR 答题面板开着，才保留鼠标（即不锁定）
            if (SceneManager.GetActiveScene().name == "StartGame" || isTutorOpen || isQuestionOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 否则才锁定鼠标（适合第一人称主馆）
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    /// <summary>
    /// 退出按钮点击逻辑。根据当前场景不同执行不同操作：
    /// - StartGame：退出游戏（编辑器下停止运行，构建后退出应用）
    /// - Museum_Main：返回开始界面
    /// - 其他展示场景：使用保险箱数据返回主馆
    /// </summary>
    public void OnExitButton()
    {
        Time.timeScale = 1f;                     // 确保时间恢复正常
        SwitchSettingPanel(false);                 // 关闭设置面板
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "StartGame")
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;  // 编辑器模式下停止运行
#else
            Application.Quit();                                 // 构建模式下退出应用
#endif
        }
        else if (currentScene == "Museum_Main")
        {
            SceneManager.LoadScene("StartGame");                // 返回开始界面
        }
        else
        {
            // 其他场景（如ImageContent, VideoContent等），返回主馆
            if (GameData.Instance)
            {
                // 如果保险箱中有数据，则将其转移到位置记忆字段，并标记需要恢复位置
                if (GameData.Instance.TempSafeState.HasData)
                {
                    var safeData = GameData.Instance.TempSafeState;
                    GameData.Instance.LastPlayerPosition = safeData.Position;
                    GameData.Instance.LastPlayerRotation = safeData.Rotation;
                    GameData.Instance.WasFirstPerson = safeData.IsFirstPerson;
                    GameData.Instance.ShouldRestorePosition = true;
                    GameData.Instance.TempSafeState.HasData = false;  // 清空保险箱
                }
                else { GameData.Instance.ShouldRestorePosition = false; }
            }
            // 通过SceneLoading加载主馆场景（会经过LoadingScene）
            SceneLoading.LoadLevel("Museum_Main");
        }
    }

    /// <summary>
    /// 保存设置（目前仅打印日志，实际数据已在实时修改中保存到GameData）
    /// </summary>
    private void SaveSettings() { Debug.Log("设置已保存"); SwitchSettingPanel(false); }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonSound()
    {
        if (GameData.Instance && GameData.Instance.ButtonClickSound)
        {
            if (uiAudioSource == null) uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource != null) uiAudioSource.PlayOneShot(GameData.Instance.ButtonClickSound, GameData.Instance.ButtonVolume);
        }
    }
}