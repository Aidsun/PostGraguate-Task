using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VideoDisplayController : MonoBehaviour
{
    [Header("组件")]
    public VideoPlayer videoPlayer;
    public Image backgroundRenderer;
    public RawImage displayScreen;
    public AutoScrollText scrollingDescription;

    private bool isUserPaused = false;
    private bool isSystemPaused = false;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. 进场景先让 BGM 闭嘴
        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        if (GameData.CurrentVideo != null)
        {
            var data = GameData.CurrentVideo;

            // 设置文字
            if (scrollingDescription)
            {
                var tmp = scrollingDescription.GetComponent<TMP_Text>();
                if (tmp) tmp.text = data.Description;
            }

            // 2. 准备视频
            if (videoPlayer)
            {
                videoPlayer.clip = data.VideoContent;

                // 【核心修复】动态寻找活着的 AudioManager
                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    // 重置音频源状态，防止静音或残留
                    AudioManager.Instance.VidSource.Stop();
                    AudioManager.Instance.VidSource.clip = null;
                    AudioManager.Instance.VidSource.volume = 1.0f; // 确保音量不是0
                    AudioManager.Instance.VidSource.mute = false;  // 确保没被静音

                    // 强制代码连接 (无视 Inspector 里的死链接)
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource);
                }

                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();
            }
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play(); // 准备好后再播放

        // 适配 UI
        if (displayScreen != null)
        {
            displayScreen.rectTransform.sizeDelta = new Vector2(vp.width, vp.height);
        }

        // 播放解说
        var data = GameData.CurrentVideo;
        float audioDuration = 10f;
        if (data.VoiceClip != null)
        {
            audioDuration = data.VoiceClip.length;
            if (AudioManager.Instance && AudioManager.Instance.DesSource && data.AutoPlayVoice)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;
                des.Play();
            }
        }

        if (scrollingDescription) scrollingDescription.StartScrollingByDuration(audioDuration);
    }

    void OnDestroy()
    {
        if (videoPlayer) videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    void Update()
    {
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;
            if (isSystemPaused != panelOpen)
            {
                isSystemPaused = panelOpen;
                RefreshPlayState();
            }
        }

        if (GameData.Instance && Input.GetKeyDown(GameData.Instance.VideoPauseKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
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
            else if (!shouldPause && des.clip != null) des.UnPause();
        }
    }
}