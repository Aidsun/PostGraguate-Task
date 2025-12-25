using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

public class VideoDisplayController : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;   // 拖入场景里的 Video Player
    public RawImage displayScreen;    // 拖入用来显示视频的 Raw Image
    public AspectRatioFitter videoFitter; //拖入 Raw Image 上的 AspectRatioFitter 组件

    [Header("UI 信息绑定")]
    public TMP_Text titleText;        // 拖入右侧的标题文本
    public TMP_Text descriptionText;  // 拖入右侧的介绍文本
    public AudioSource descriptionAudio;

    [Header("控制按钮")]
    [Tooltip("暂停按钮")]
    public Button pauseButton;

    // 内部状态
    private bool isPrepared = false;
    // 视频音量（从设置面板获取）
    private float currentVideoVolume = 1.0f;
    // 解说音量（从设置面板获取）
    private float currentDescriptionVolume = 1.0f;

    void Awake()
    {
        // 注册到设置面板，接收配置更新
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
        // 应用当前设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        // 1. 读取全局数据
        var data = GameDate.CurrentVideoDate;

        if (data != null)
        {
            // 设置文字内容
            if (titleText) titleText.text = "《" + data.Title + "》";
            if (descriptionText) descriptionText.text = data.DescriptionText;

            // 设置并播放解说音频
            if (descriptionAudio && data.DescriptionAudio)
            {
                descriptionAudio.clip = data.DescriptionAudio;
                descriptionAudio.volume = currentDescriptionVolume; // 应用音量设置
                descriptionAudio.Play();
            }

            // 设置视频并准备播放
            if (videoPlayer && data.VideoFile)
            {
                videoPlayer.clip = data.VideoFile;

                // 监听视频准备完成事件，用于调整宽高比
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare(); // 开始准备
            }
        }
        else
        {
            Debug.LogError("【错误】没有读取到视频数据，请从浏览馆入口进入！");
        }

        // 增加按钮暂停功能
        if (pauseButton) pauseButton.onClick.AddListener(TogglePlayPause);
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        // 应用视频音量
        currentVideoVolume = settings.videoVolume;

        // 应用解说音量
        currentDescriptionVolume = settings.descriptionVolume;

        // 更新已初始化的音频音量
        if (descriptionAudio != null)
        {
            descriptionAudio.volume = currentDescriptionVolume;
        }

        // 更新已初始化的视频音量
        if (videoPlayer != null && isPrepared)
        {
            SetVideoPlayerVolume(currentVideoVolume);
        }

        Debug.Log($"VideoDisplayController: 应用设置 - 视频音量: {currentVideoVolume}, 解说音量: {currentDescriptionVolume}");
    }

    // 视频准备好后的回调
    void OnVideoPrepared(VideoPlayer vp)
    {
        isPrepared = true;

        // 设置视频音量
        SetVideoPlayerVolume(currentVideoVolume);

        vp.Play(); // 自动开始播放

        // 【核心】根据视频源的宽高，动态设置 RawImage 的比例
        if (videoFitter != null)
        {
            // 比如 1920/1080 = 1.777
            videoFitter.aspectRatio = (float)vp.width / vp.height;
        }
    }

    // 设置视频播放器音量 - 兼容不同Unity版本的方法
    private void SetVideoPlayerVolume(float volume)
    {
        if (videoPlayer != null)
        {
            // 方法1：使用Direct Audio Volume（适用于Direct输出模式）
            if (videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
            {
                // 设置所有音频轨道的音量
                for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
                {
                    videoPlayer.SetDirectAudioVolume(i, volume);
                }
            }
        }
    }


    void Update()
    {
        // 监听空格键暂停/继续
        if (isPrepared && Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }

        // 如果设置面板打开，暂停视频播放
        if (SettingPanel.Instance != null && SettingPanel.Instance.isPanelActive && videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    void TogglePlayPause()
    {
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            else
            {
                videoPlayer.Play();
            }
        }
    }
}