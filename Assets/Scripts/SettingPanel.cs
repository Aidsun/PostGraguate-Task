using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SettingPanel : MonoBehaviour
{
    public static SettingPanel Instance;

    [Header("【核心组件】")]
    public GameObject panelRoot;

    [Space(10)]
    [Header("=== 🎮 控制设置 UI ===")]
    public TMP_Dropdown viewKeyDropdown;
    public TMP_Dropdown callPanelDropdown;
    public TMP_Dropdown videoControlDropdown;

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

    // 确保默认为 false
    [HideInInspector] public bool isPanelActive = false;

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
        // 严格的单例模式保护
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // 这里的 return 很重要，防止后续代码执行
        }
    }

    private void Start()
    {
        SetupPanelLayer();
        if (panelRoot != null) panelRoot.SetActive(false);
        isPanelActive = false;

        // 强制重置时间，防止因为异常退出导致的卡死
        Time.timeScale = 1f;

        InitUI();
        BindEvents();
    }

    private void SetupPanelLayer()
    {
        if (panelRoot == null) return;
        Canvas cv = panelRoot.GetComponent<Canvas>();
        if (cv == null) cv = panelRoot.AddComponent<Canvas>();
        // 提高SortingOrder，确保面板永远在最上层，不会被其他UI遮挡导致点击不到
        cv.overrideSorting = true;
        cv.sortingOrder = 9999;
        if (panelRoot.GetComponent<GraphicRaycaster>() == null) panelRoot.AddComponent<GraphicRaycaster>();
        if (panelRoot.GetComponent<CanvasGroup>() == null) panelRoot.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        // 如果在 Loading 界面，禁止呼出
        if (SceneManager.GetActiveScene().name == "LoadingScene") return;

        // 【核心修复】将按键检测放在 Update 最顶层
        // 确保 KeyConfig.CallPanelKey 有值，如果没有则默认为 Tab
        KeyCode callKey = KeyConfig.CallPanelKey == KeyCode.None ? KeyCode.Tab : KeyConfig.CallPanelKey;

        if (Input.GetKeyDown(callKey))
        {
            SwitchSettingPanel(!isPanelActive);
        }
    }

    private void InitUI()
    {
        if (GameData.Instance != null)
        {
            if (bgmVolumeSlider) bgmVolumeSlider.value = GameData.Instance.BgmVolume;
            if (videoVolumeSlider) videoVolumeSlider.value = GameData.Instance.VideoVolume;
            if (descriptionVolumeSlider) descriptionVolumeSlider.value = GameData.Instance.VoiceVolume;
            if (buttonVolumeSlider) buttonVolumeSlider.value = GameData.Instance.ButtonVolume;

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
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener((v) => { if (GameData.Instance) GameData.Instance.BgmVolume = v; });
        if (videoVolumeSlider) videoVolumeSlider.onValueChanged.AddListener((v) => { if (GameData.Instance) GameData.Instance.VideoVolume = v; });
        if (descriptionVolumeSlider) descriptionVolumeSlider.onValueChanged.AddListener((v) => { if (GameData.Instance) GameData.Instance.VoiceVolume = v; });
        if (buttonVolumeSlider) buttonVolumeSlider.onValueChanged.AddListener((v) => { if (GameData.Instance) GameData.Instance.ButtonVolume = v; });

        if (moveSpeedInput) moveSpeedInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) GameData.Instance.MoveSpeed = v; });
        if (jumpHeightInput) jumpHeightInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) GameData.Instance.JumpHeight = v; });
        if (interactionDistInput) interactionDistInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) GameData.Instance.InteractionDistance = v; });
        if (stepDistInput) stepDistInput.onEndEdit.AddListener((str) => { if (float.TryParse(str, out float v)) GameData.Instance.StepDistance = v; });

        if (viewKeyDropdown) viewKeyDropdown.onValueChanged.AddListener((idx) => { KeyConfig.ViewSwitchKey = dropdownKeys[idx]; });
        if (callPanelDropdown) callPanelDropdown.onValueChanged.AddListener((idx) => { KeyConfig.CallPanelKey = dropdownKeys[idx]; });
        if (videoControlDropdown) videoControlDropdown.onValueChanged.AddListener((idx) => { if (GameData.Instance) GameData.Instance.VideoPauseKey = dropdownKeys[idx]; });

        if (saveButton) saveButton.onClick.AddListener(SaveSettings);
        if (exitButton) exitButton.onClick.AddListener(OnExitButton);
    }

    private void UpdateDropdownSelection(TMP_Dropdown dropdown, KeyCode currentKey)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(dropdownKeys.Select(k => k.ToString()).ToList());
        int index = dropdownKeys.IndexOf(currentKey);
        if (index >= 0) dropdown.value = index;
    }

    public void SwitchSettingPanel(bool isOpen)
    {
        isPanelActive = isOpen;

        if (panelRoot)
            panelRoot.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            // 只有在非 StartGame 场景才锁定鼠标
            if (SceneManager.GetActiveScene().name == "StartGame")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // 注意：如果你在视频界面也希望显示鼠标，这里可能需要加个判断
                // 比如： if (SceneManager.GetActiveScene().name == "VideoDisplay") ...
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
            if (GameData.Instance) GameData.Instance.ShouldRestorePosition = true;
            SceneLoading.LoadLevel("Museum_Main");
        }
    }

    private void SaveSettings()
    {
        Debug.Log("设置已保存");
        SwitchSettingPanel(false);
    }
}