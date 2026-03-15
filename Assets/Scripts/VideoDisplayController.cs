// 文件：VideoDisplayController.cs
// 模块：展示场景 / 视频展示
// 说明：该脚本负责视频展示场景（VideoContent）的控制。它从GameData.CurrentVideo获取展品数据，
//      准备并播放视频，同时管理解说音频的播放（与视频同步或独立），支持用户手动暂停/继续，
//      以及响应设置面板的打开/关闭进行系统级暂停。视频画面通过RawImage显示，并调整其尺寸匹配视频。
//      还负责将视频音频路由到AudioManager的VidSource，确保音量控制统一。
// 特性：使用VideoPlayer组件，通过prepareCompleted事件处理视频准备完成后的操作，
//      使用协程（未显式使用，但通过事件驱动），与SettingPanel交互处理系统暂停，
//      通过GameData获取视频数据和用户自定义的暂停键。

using UnityEngine;
using UnityEngine.Video;      // 使用VideoPlayer
using UnityEngine.UI;         // 使用Image, RawImage
using TMPro;                  // 使用TextMeshPro
using System.Collections;     // 使用协程（虽未直接使用，但保留）

public class VideoDisplayController : MonoBehaviour
{
    [Header("组件")]           // Inspector分组
    public VideoPlayer videoPlayer;              // 视频播放器组件
    public Image backgroundRenderer;              // 背景图片渲染器（随机背景）
    public RawImage displayScreen;                // 用于显示视频画面的RawImage
    public AutoScrollText scrollingDescription;   // 滚动文本组件（用于显示描述文字）

    private bool isUserPaused = false;   // 用户手动暂停标志（通过按键触发）
    private bool isSystemPaused = false;  // 系统暂停标志（例如设置面板打开时）

    void Start()
    {
        // 解锁并显示鼠标（视频展示场景通常需要鼠标操作）
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. 进场景先让背景音乐暂停，避免干扰视频声音
        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        // 检查是否有当前视频展品数据
        if (GameData.CurrentVideo != null)
        {
            var data = GameData.CurrentVideo;   // 获取数据包

            // 设置滚动描述文字的文本内容
            if (scrollingDescription)
            {
                // 获取AutoScrollText物体上的TMP_Text组件并赋值
                var tmp = scrollingDescription.GetComponent<TMP_Text>();
                if (tmp) tmp.text = data.Description;
            }

            // 2. 准备视频
            if (videoPlayer)
            {
                // 直接赋值视频剪辑（注意：此处在热更新改造前使用直接引用）
                videoPlayer.clip = data.VideoContent;

                // 【核心修复】动态连接视频音频到AudioManager的VidSource
                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    // 重置音频源状态，防止静音或残留
                    AudioManager.Instance.VidSource.Stop();
                    AudioManager.Instance.VidSource.clip = null;
                    AudioManager.Instance.VidSource.volume = 1.0f; // 确保音量不是0
                    AudioManager.Instance.VidSource.mute = false;  // 确保没被静音

                    // 强制代码连接 (无视 Inspector 里的死链接)
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource; // 设置音频输出模式为AudioSource
                    videoPlayer.EnableAudioTrack(0, true);                         // 启用第一音轨
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource); // 指定目标音频源
                }

                // 注册视频准备完成事件，准备完成后自动播放
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();   // 开始准备视频（缓冲等）
            }
        }
    }

    /// <summary>
    /// 视频准备完成时的回调。在此处开始播放视频，并调整显示画面尺寸，启动解说音频和滚动文本。
    /// </summary>
    /// <param name="vp">触发事件的VideoPlayer（此处未使用）</param>
    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play(); // 准备好后开始播放

        // 适配UI：将RawImage的尺寸调整为视频的原始宽高，保持比例
        if (displayScreen != null)
        {
            displayScreen.rectTransform.sizeDelta = new Vector2(vp.width, vp.height);
        }

        // 播放解说音频
        var data = GameData.CurrentVideo;
        float audioDuration = 10f; // 默认时长，若没有解说则使用默认值
        if (data.VoiceClip != null)
        {
            audioDuration = data.VoiceClip.length; // 获取解说音频时长
            // 如果设置了自动播放解说
            if (AudioManager.Instance && AudioManager.Instance.DesSource && data.AutoPlayVoice)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;   // 设置解说音频剪辑
                des.Play();                   // 立即播放解说
            }
        }

        // 启动滚动文本，传入音频时长以计算滚动速度
        if (scrollingDescription) scrollingDescription.StartScrollingByDuration(audioDuration);
    }

    // 当对象被销毁时，取消事件订阅，防止内存泄漏
    void OnDestroy()
    {
        if (videoPlayer) videoPlayer.prepareCompleted -= OnVideoPrepared;
    }

    void Update()
    {
        // 监听设置面板状态，控制系统暂停
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive; // 获取面板是否打开
            if (isSystemPaused != panelOpen) // 如果系统暂停状态与面板状态不一致，则更新
            {
                isSystemPaused = panelOpen;
                RefreshPlayState(); // 刷新播放状态
            }
        }

        // 检测用户自定义的暂停键（从GameData读取）
        if (GameData.Instance && Input.GetKeyDown(GameData.Instance.VideoPauseKey))
        {
            TogglePause(); // 切换用户暂停状态
        }
    }

    /// <summary>
    /// 切换用户手动暂停状态。
    /// </summary>
    public void TogglePause()
    {
        isUserPaused = !isUserPaused;
        RefreshPlayState(); // 刷新播放状态
    }

    /// <summary>
    /// 根据当前用户暂停和系统暂停的综合状态，决定视频和解说音频是否暂停。
    /// </summary>
    void RefreshPlayState()
    {
        bool shouldPause = isUserPaused || isSystemPaused; // 任一为真则暂停

        // 控制视频播放器
        if (videoPlayer)
        {
            if (shouldPause && videoPlayer.isPlaying) videoPlayer.Pause();
            else if (!shouldPause && !videoPlayer.isPlaying) videoPlayer.Play();
        }

        // 控制解说音频
        if (AudioManager.Instance && AudioManager.Instance.DesSource)
        {
            AudioSource des = AudioManager.Instance.DesSource;
            if (shouldPause && des.isPlaying) des.Pause();
            else if (!shouldPause && des.clip != null) des.UnPause(); // 恢复播放
        }
    }
}