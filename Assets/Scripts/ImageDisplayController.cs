using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ImageDisplayController : MonoBehaviour
{
    [Header("UI 组件绑定")]
    public TMP_Text imageTitle;
    public Image imageShow;
    public TMP_Text imageDescription;

    [Header("图片解说")]
    public AudioSource imageAudio;

    [Header("退出设置")]
    public Button exitButton;
    public string returnSceneName = "Museum_Main";

    // 当前解说音量
    private float currentDescriptionVolume = 1.0f;
    // 记录是否因为面板打开而暂停了音频
    private bool isPausedByPanel = false;

    void Awake()
    {
        // 注册到设置面板
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        // 注销设置应用方法
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void Start()
    {
        // 1. 应用初始设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        // 2. 获取数据
        var data = GameDate.CurrentImageData;

        if (data != null)
        {
            if (imageTitle) imageTitle.text = "《" + data.Title + "》";

            // 【关键修正】这里改为 ImageFile，匹配 GameDate 定义
            if (imageShow && data.ImageFile)
                imageShow.sprite = data.ImageFile;

            if (imageDescription)
                imageDescription.text = data.DescriptionText;

            // 播放解说
            if (imageAudio && data.DescriptionAudio)
            {
                imageAudio.clip = data.DescriptionAudio;
                imageAudio.volume = currentDescriptionVolume;
                imageAudio.Play();
            }
        }
        else
        {
            Debug.LogError("图片展示场景：未能获取到数据！请从博物馆入口进入。");
        }

        // 3. 绑定退出按钮
        if (exitButton)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        // 4. 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        currentDescriptionVolume = settings.descriptionVolume;

        // 实时更新音量
        if (imageAudio != null)
        {
            imageAudio.volume = currentDescriptionVolume;
        }

        Debug.Log($"ImageDisplayController: 同步音量: {currentDescriptionVolume}");
    }

    void Update()
    {
        // 面板互斥逻辑：面板打开时暂停音频
        if (SettingPanel.Instance != null)
        {
            if (SettingPanel.Instance.isPanelActive)
            {
                if (imageAudio != null && imageAudio.isPlaying)
                {
                    imageAudio.Pause();
                    isPausedByPanel = true;
                }
            }
            else
            {
                // 面板关闭后，如果之前是因为面板暂停的，则恢复
                if (isPausedByPanel)
                {
                    if (imageAudio != null) imageAudio.UnPause();
                    isPausedByPanel = false;
                }
            }
        }

        // 额外的快捷键支持 (空格键重播)
        if (Input.GetKeyDown(KeyCode.Space) && imageAudio != null && imageAudio.clip != null)
        {
            if (imageAudio.isPlaying)
                imageAudio.Stop();

            imageAudio.Play();
        }

        // ESC 键退出已经在 SettingPanel 中处理了，但为了保险起见，
        // 如果这里没有 SettingPanel，可以加一个后备退出逻辑 (可选)
    }

    void OnExitButtonClicked()
    {
        // 1. 停止音频
        if (imageAudio) imageAudio.Stop();

        // 2. 【核心】标记需要恢复位置
        GameDate.ShouldRestorePosition = true;
        Debug.Log("图片展示：退出并请求位置恢复");

        // 3. 安全跳转
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(returnSceneName);
        else
            SceneManager.LoadScene(returnSceneName);
    }
}