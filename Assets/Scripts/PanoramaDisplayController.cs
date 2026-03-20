// 文件：PanoramaDisplayController.cs
// 模块：展示场景 / 全景展示
// 说明：该脚本负责全景展示场景（PanoramaContent）的控制。它从GameData.CurrentPanorama获取展品数据，
//      创建RenderTexture并将全景视频渲染到天空盒材质上，实现360度沉浸式观看。同时处理音频路由，
//      将视频声音输出到VidSource，将解说音频输出到DesSource（延迟3秒播放），
//      并监听设置面板状态以暂停/恢复视频和解说音频。
// 特性：使用RenderTexture动态渲染，修改RenderSettings.skybox实现天空盒替换，
//      通过协程实现解说音频延迟播放，OnDestroy释放RenderTexture资源并停止音频。

using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class PanoramaDisplayController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Material skyboxMat;
    public TMP_Text titleText;

    private RenderTexture rt;
    private bool isPaused = false;

    void Start()
    {
        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        if (GameData.CurrentPanorama != null)
        {
            var data = GameData.CurrentPanorama;
            if (titleText) titleText.text = data.Title;

            if (videoPlayer)
            {
                rt = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = rt;
                if (skyboxMat)
                {
                    skyboxMat.SetTexture("_MainTex", rt);
                    RenderSettings.skybox = skyboxMat;
                }
                videoPlayer.clip = data.PanoramaContent;

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

                videoPlayer.Play();
            }

            if (data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;
                if (data.AutoPlayVoice) StartCoroutine(PlayVoiceWithDelay(des, 3.0f));
            }
        }
    }

    IEnumerator PlayVoiceWithDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isPaused && source && source.clip != null) source.Play();
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();

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
            if (panelOpen && !isPaused)
            {
                if (videoPlayer.isPlaying) videoPlayer.Pause();
                if (AudioManager.Instance.DesSource.isPlaying) AudioManager.Instance.DesSource.Pause();
                isPaused = true;
            }
            else if (!panelOpen && isPaused)
            {
                videoPlayer.Play();
                if (AudioManager.Instance.DesSource.clip != null) AudioManager.Instance.DesSource.UnPause();
                isPaused = false;
            }
        }
    }
}