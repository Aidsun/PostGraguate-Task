// 文件：PanoramaDisplayController.cs
// 模块：展示场景 / 全景展示
// 说明：该脚本负责全景展示场景（PanoramaContent）的控制。它从GameData.CurrentPanorama获取展品数据，
//      创建RenderTexture并将全景视频渲染到天空盒材质上，实现360度沉浸式观看。同时处理音频路由，
//      将视频声音输出到VidSource，将解说音频输出到DesSource（延迟3秒播放），
//      并监听设置面板状态以暂停/恢复视频和解说音频。
// 特性：使用RenderTexture动态渲染，修改RenderSettings.skybox实现天空盒替换，
//      通过协程实现解说音频延迟播放，OnDestroy释放RenderTexture资源。

using UnityEngine;
using UnityEngine.Video;      // 使用VideoPlayer
using TMPro;                  // 使用TextMeshPro
using System.Collections;     // 使用协程

public class PanoramaDisplayController : MonoBehaviour
{
    // 组件绑定，需在Inspector中拖拽赋值
    public VideoPlayer videoPlayer;       // 用于播放全景视频的VideoPlayer组件
    public Material skyboxMat;            // 用于显示全景视频的天空盒材质
    public TMP_Text titleText;            // 显示全景标题的文本框

    // 私有变量
    private RenderTexture rt;             // 渲染纹理，VideoPlayer将视频渲染到此纹理上
    private bool isPaused = false;        // 标记是否因设置面板打开而暂停

    void Start()
    {
        // 1. 强制暂停背景音乐（全景场景不需要背景音乐）
        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        // 检查是否有当前全景展品数据
        if (GameData.CurrentPanorama != null)
        {
            var data = GameData.CurrentPanorama;   // 获取数据包
            // 设置标题
            if (titleText) titleText.text = data.Title;

            if (videoPlayer)
            {
                // 创建RenderTexture，分辨率为4096x2048（标准全景视频分辨率），无深度缓冲
                rt = new RenderTexture(4096, 2048, 0);
                // 设置VideoPlayer将视频渲染到该RenderTexture
                videoPlayer.targetTexture = rt;

                // 如果提供了天空盒材质，将RenderTexture赋值给材质的_MainTex属性
                if (skyboxMat)
                {
                    skyboxMat.SetTexture("_MainTex", rt);
                    // 将当前场景的天空盒替换为该材质
                    RenderSettings.skybox = skyboxMat;
                }

                // 设置视频剪辑（注意：此处使用data.PanoramaContent，直接引用VideoClip）
                videoPlayer.clip = data.PanoramaContent;

                // 【核心修复】动态连接视频音频到AudioManager的VidSource
                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    // 重置音频源状态，避免影响
                    AudioManager.Instance.VidSource.Stop();
                    AudioManager.Instance.VidSource.clip = null;
                    AudioManager.Instance.VidSource.volume = 1.0f;
                    AudioManager.Instance.VidSource.mute = false;

                    // 配置VideoPlayer的音频输出模式为AudioSource，并指定目标音频源
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);               // 启用第一音轨
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource);
                }

                // 开始播放全景视频
                videoPlayer.Play();
            }

            // 播放解说音频（如果有）
            if (data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;   // 设置解说音频剪辑
                // 如果设置了自动播放，启动延迟播放协程（延迟3秒）
                if (data.AutoPlayVoice) StartCoroutine(PlayVoiceWithDelay(des, 3.0f));
            }
        }
    }

    /// <summary>
    /// 延迟播放解说音频的协程。
    /// 等待指定秒数后，如果未被暂停且音频源有效，则播放。
    /// </summary>
    /// <param name="source">要播放的AudioSource</param>
    /// <param name="delay">延迟秒数</param>
    IEnumerator PlayVoiceWithDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        // 检查暂停状态和音频源有效性
        if (!isPaused && source && source.clip != null) source.Play();
    }

    // 当对象被销毁时，释放RenderTexture资源
    void OnDestroy()
    {
        if (rt != null) rt.Release();   // 释放GPU资源
    }

    void Update()
    {
        // 监听设置面板状态，控制全景视频和解说音频的暂停/恢复
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;   // 获取设置面板是否打开

            // 如果面板打开且当前未标记为暂停
            if (panelOpen && !isPaused)
            {
                // 如果视频正在播放，则暂停
                if (videoPlayer.isPlaying) videoPlayer.Pause();
                // 如果解说音频正在播放，则暂停
                if (AudioManager.Instance.DesSource.isPlaying) AudioManager.Instance.DesSource.Pause();
                isPaused = true;   // 标记为暂停状态
            }
            // 如果面板关闭且当前标记为暂停
            else if (!panelOpen && isPaused)
            {
                // 恢复视频播放
                videoPlayer.Play();
                // 如果解说音频剪辑存在，则恢复播放（从暂停处继续）
                if (AudioManager.Instance.DesSource.clip != null) AudioManager.Instance.DesSource.UnPause();
                isPaused = false;   // 取消暂停标记
            }
        }
    }
}