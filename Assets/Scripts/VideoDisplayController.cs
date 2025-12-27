using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class VideoDisplayController : MonoBehaviour
{
    [Header("组件")]
    public VideoPlayer videoPlayer;
    public Image backgroundRenderer;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Button pauseButton;

    private bool isUserPaused = false;
    private bool isSystemPaused = false;

    void Start()
    {
        if (GameData.Instance && backgroundRenderer)
            backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();

        if (GameData.CurrentVideo != null)
        {
            var data = GameData.CurrentVideo;
            if (titleText) titleText.text = data.Title;
            if (descriptionText) descriptionText.text = data.Description;

            // 1. 播放视频 (路由声音到 VidAudio)
            if (videoPlayer)
            {
                videoPlayer.clip = data.VideoContent;

                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource);
                }
                videoPlayer.Play();
            }

            // 2. 播放解说 (使用 DesAudio)
            if (data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                AudioSource desSource = AudioManager.Instance.DesSource;
                desSource.clip = data.VoiceClip;
                if (data.AutoPlayVoice) desSource.Play();
            }
        }

        if (pauseButton) pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    void Update()
    {
        // 监测控制面板状态
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;
            if (isSystemPaused != panelOpen)
            {
                isSystemPaused = panelOpen;
                RefreshPlayState();
            }
        }

        // 监测按键
        if (GameData.Instance && Input.GetKeyDown(GameData.Instance.VideoPauseKey))
        {
            OnPauseButtonClicked();
        }
    }

    public void OnPauseButtonClicked()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
        isUserPaused = !isUserPaused;
        RefreshPlayState();
    }

    void RefreshPlayState()
    {
        bool shouldPause = isUserPaused || isSystemPaused;

        if (videoPlayer)
        {
            if (shouldPause && videoPlayer.isPlaying) videoPlayer.Pause();
            else if (!shouldPause && !videoPlayer.isPlaying) videoPlayer.Play();
        }

        if (AudioManager.Instance && AudioManager.Instance.DesSource)
        {
            AudioSource des = AudioManager.Instance.DesSource;
            if (shouldPause && des.isPlaying) des.Pause();
            else if (!shouldPause && !des.isPlaying && des.clip != null) des.UnPause();
        }
    }
}