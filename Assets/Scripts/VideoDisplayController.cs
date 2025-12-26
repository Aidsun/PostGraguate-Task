using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class VideoDisplayController : MonoBehaviour
{
    [Header("组件")]
    public VideoPlayer videoPlayer;
    public Image backgroundRenderer;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public AudioSource voiceSource;
    public Button pauseButton;

    // 分离状态：用户是否想暂停 vs 系统是否强制暂停
    private bool isUserPaused = false;
    private bool isSystemPaused = false;

    void Start()
    {
        if (GameData.Instance && backgroundRenderer) backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();

        if (GameData.CurrentVideo != null)
        {
            var data = GameData.CurrentVideo;
            if (titleText) titleText.text = data.Title;
            if (descriptionText) descriptionText.text = data.Description;

            if (videoPlayer) { videoPlayer.clip = data.VideoContent; videoPlayer.Play(); }
            if (data.AutoPlayVoice && data.VoiceClip != null && voiceSource) { voiceSource.clip = data.VoiceClip; voiceSource.Play(); }
        }

        if (pauseButton) pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    void Update()
    {
        // 1. 同步音量
        if (GameData.Instance)
        {
            if (videoPlayer) videoPlayer.SetDirectAudioVolume(0, GameData.Instance.VideoVolume);
            if (voiceSource) voiceSource.volume = GameData.Instance.VoiceVolume;

            // 2. 检测用户按键输入
            if (Input.GetKeyDown(GameData.Instance.VideoPauseKey))
            {
                OnPauseButtonClicked();
            }
        }

        // 3. 检测系统面板状态 (设置面板打开时强制暂停)
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;

            // 如果面板状态改变了，更新系统暂停状态
            if (isSystemPaused != panelOpen)
            {
                isSystemPaused = panelOpen;
                RefreshPlayState(); // 状态改变时刷新播放/暂停
            }
        }
    }

    // 用户手动点击按钮或按键
    public void OnPauseButtonClicked()
    {
        isUserPaused = !isUserPaused; // 切换用户意愿
        RefreshPlayState();
    }

    // 核心逻辑：根据用户意愿和系统状态，决定最终是播还是停
    void RefreshPlayState()
    {
        // 只要 “用户想暂停” 或者 “系统强制暂停(面板打开)”，就必须暂停
        bool shouldPause = isUserPaused || isSystemPaused;

        if (shouldPause)
        {
            if (videoPlayer && videoPlayer.isPlaying) videoPlayer.Pause();
            if (voiceSource) voiceSource.Pause();
        }
        else
        {
            if (videoPlayer && !videoPlayer.isPlaying) videoPlayer.Play();
            if (voiceSource) voiceSource.UnPause();
        }
    }
}