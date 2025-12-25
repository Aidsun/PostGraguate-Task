using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

public class VideoDisplayController : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;       // 场景里的 Video Player
    public RawImage displayScreen;        // 显示视频的 Raw Image
    public AspectRatioFitter videoFitter; // 控制比例的组件

    [Header("UI 信息绑定")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public AudioSource descriptionAudio;  // 用于播放解说

    [Header("控制按钮")]
    public Button exitButton;
    public Button pauseButton;

    [Header("设置")]
    public string returnSceneName = "Museum_Main";

    // 内部状态
    private bool isPrepared = false;
    private float currentVideoVolume = 1.0f;
    private float currentDescriptionVolume = 1.0f;
    private bool isPausedByPanel = false; // 是否因为面板打开而暂停

    void Awake()
    {
        // 注册设置监听
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
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

        // 2. 读取数据
        var data = GameDate.CurrentVideoDate;

        if (data != null)
        {
            if (titleText) titleText.text = "《" + data.Title + "》";
            if (descriptionText) descriptionText.text = data.DescriptionText;

            // 播放解说
            if (descriptionAudio && data.DescriptionAudio)
            {
                descriptionAudio.clip = data.DescriptionAudio;
                descriptionAudio.volume = currentDescriptionVolume;
                descriptionAudio.Play();
            }

            // 准备视频
            if (videoPlayer && data.VideoFile)
            {
                videoPlayer.clip = data.VideoFile;
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();
            }
        }
        else
        {
            Debug.LogError("未读取到视频数据！");
        }

        // 3. 绑定按钮
        if (exitButton) exitButton.onClick.AddListener(OnExitButtonClicked);
        if (pauseButton) pauseButton.onClick.AddListener(TogglePlayPause);

        // 4. 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        currentVideoVolume = settings.videoVolume;
        currentDescriptionVolume = settings.descriptionVolume;

        // 更新解说音量
        if (descriptionAudio != null)
        {
            descriptionAudio.volume = currentDescriptionVolume;
        }

        // 更新视频音量
        if (videoPlayer != null && isPrepared)
        {
            SetVideoPlayerVolume(currentVideoVolume);
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        isPrepared = true;
        SetVideoPlayerVolume(currentVideoVolume);
        vp.Play();

        // 动态调整比例
        if (videoFitter != null)
        {
            videoFitter.aspectRatio = (float)vp.width / vp.height;
        }
    }

    void Update()
    {
        // 空格键暂停/播放
        if (isPrepared && Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        // 面板互斥逻辑
        if (SettingPanel.Instance != null)
        {
            if (SettingPanel.Instance.isPanelActive)
            {
                // 面板打开时：强制暂停
                if (!isPausedByPanel)
                {
                    if (videoPlayer.isPlaying) videoPlayer.Pause();
                    if (descriptionAudio && descriptionAudio.isPlaying) descriptionAudio.Pause();
                    isPausedByPanel = true;
                }
            }
            else
            {
                // 面板关闭后：如果之前是因为面板暂停的，则恢复
                if (isPausedByPanel)
                {
                    if (!videoPlayer.isPlaying) videoPlayer.Play();
                    // 只有当音频还没播完时才恢复
                    if (descriptionAudio && !descriptionAudio.isPlaying && descriptionAudio.time > 0)
                        descriptionAudio.UnPause();

                    isPausedByPanel = false;
                }
            }
        }
    }

    void TogglePlayPause()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
        else
            videoPlayer.Play();
    }

    void SetVideoPlayerVolume(float volume)
    {
        if (videoPlayer != null)
        {
            // 兼容 Direct 模式
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume(i, volume);
            }
        }
    }

    void OnExitButtonClicked()
    {
        if (videoPlayer) videoPlayer.Stop();
        if (descriptionAudio) descriptionAudio.Stop();

        // 【核心】标记位置恢复
        GameDate.ShouldRestorePosition = true;
        Debug.Log("视频展示：退出并请求位置恢复");

        // 安全跳转
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(returnSceneName);
        else
            SceneManager.LoadScene(returnSceneName);
    }
}