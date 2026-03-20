// 文件：VideoDisplayController.cs
// 模块：展示场景 / 视频展示
// 说明：该脚本负责视频展示场景（VideoContent）的控制。它从GameData.CurrentVideo获取展品数据，
//      准备并播放视频，同时管理解说音频的播放（与视频同步或独立），支持用户手动暂停/继续，
//      以及响应设置面板的打开/关闭进行系统级暂停。视频画面通过RawImage显示，并自动调整为视频原始分辨率。
//      还负责将视频音频路由到AudioManager的VidSource，确保音量控制统一。
// 特性：使用VideoPlayer组件，通过prepareCompleted事件处理视频准备完成后的操作，
//      与SettingPanel交互处理系统暂停，通过GameData获取视频数据和用户自定义的暂停键。

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

        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        if (GameData.CurrentVideo != null)
        {
            var data = GameData.CurrentVideo;

            if (scrollingDescription)
            {
                var tmp = scrollingDescription.GetComponent<TMP_Text>();
                if (tmp) tmp.text = data.Description;
            }

            if (videoPlayer)
            {
                videoPlayer.clip = data.VideoContent;

                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    AudioManager.Instance.VidSource.Stop();
                    AudioManager.Instance.VidSource.clip = null;
                    AudioManager.Instance.VidSource.volume = 1.0f;
                    AudioManager.Instance.VidSource.mute = false;

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
        vp.Play();

        // 获取视频原始宽高
        uint width = vp.width;
        uint height = vp.height;

        Debug.Log($"[VideoDisplay] 视频原始分辨率: {width} x {height}");

        if (displayScreen != null && width > 0 && height > 0)
        {
            // 设置RawImage尺寸为视频原始分辨率
            displayScreen.rectTransform.sizeDelta = new Vector2(width, height);

            // 强制刷新布局，防止父物体干扰
            LayoutRebuilder.ForceRebuildLayoutImmediate(displayScreen.rectTransform);
        }

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

        // 停止并清理视频音频和解说音频
        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.VidSource != null)
            {
                AudioManager.Instance.VidSource.Stop();
                AudioManager.Instance.VidSource.clip = null;
            }
            if (AudioManager.Instance.DesSource != null)
            {
                AudioManager.Instance.DesSource.Stop();
                AudioManager.Instance.DesSource.clip = null;
            }
        }
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