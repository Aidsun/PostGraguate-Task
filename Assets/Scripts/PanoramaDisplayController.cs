using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PanoramaDisplayController : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;
    public Material skyboxMaterial;

    [Header("音频组件")]
    public AudioSource audioSource;

    // 渲染纹理
    private RenderTexture panoramaRT;

    // 全景视频播放状态
    private bool isPaused;

    void Start()
    {
        // 注册设置应用方法
        SettingPanel.RegisterApplyMethod(ApplySettings);

        var data = GameDate.CurrentPanoramaDate;

        if (data != null)
        {
            if (videoPlayer && data.panoramaFile)
            {
                videoPlayer.clip = data.panoramaFile;
                panoramaRT = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = panoramaRT;

                if (skyboxMaterial != null)
                {
                    skyboxMaterial.SetTexture("_MainTex", panoramaRT);
                    RenderSettings.skybox = skyboxMaterial;
                }
                videoPlayer.Play();

                // 初始音量设置
                ApplySettings(SettingPanel.CurrentSettings);
            }

            // 播放音频逻辑
            if (audioSource != null && data.DescriptionAudio != null)
            {
                audioSource.clip = data.DescriptionAudio;
                audioSource.Play();

                // 初始音量设置
                ApplySettings(SettingPanel.CurrentSettings);
            }

            // 初始状态为播放（未暂停）
            isPaused = false;
        }
        else
        {
            Debug.LogError("没有全景视频数据！");
        }
    }

    void Update()
    {
        HandlePauseInput();

        // 检测设置面板是否打开
        if (SettingPanel.Instance != null && SettingPanel.Instance.isPanelActive)
        {
            // 如果设置面板打开，暂停视频和音频
            if (videoPlayer && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }
            if (audioSource && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    void HandlePauseInput()
    {
        // 使用设置面板中的按键配置
        if (Input.GetKeyDown(SettingPanel.CurrentSettings.callSettingPanelKey))
        {
            if (!isPaused)
            {
                // 暂停播放
                PausePlayback();
                isPaused = true;
            }
            else
            {
                // 恢复播放
                ResumePlayback();
                isPaused = false;
            }
        }
    }

    void PausePlayback()
    {
        if (videoPlayer)
        {
            videoPlayer.Pause();
        }
        if (audioSource)
        {
            audioSource.Pause();
        }
    }

    void ResumePlayback()
    {
        if (videoPlayer)
        {
            videoPlayer.Play();
        }
        if (audioSource)
        {
            audioSource.UnPause();
        }
    }

    // 应用设置的方法（供SettingPanel调用）
    public void ApplySettings(SettingPanel.SettingDate settings)
    {
        if (videoPlayer != null)
        {
            // 设置视频音量
            videoPlayer.SetDirectAudioVolume(0, settings.videoVolume);
        }

        if (audioSource != null)
        {
            // 设置解说音量
            audioSource.volume = settings.descriptionVolume;
        }

        // 设置整体音量（如果需要）
        AudioListener.volume = settings.bgmVolume; // 或者使用单独的主音量控制
    }

    // 清理资源
    void OnDestroy()
    {
        // 取消注册设置应用方法
        SettingPanel.UnregisterApplyMethod(ApplySettings);

        if (panoramaRT != null)
        {
            panoramaRT.Release();
            Destroy(panoramaRT);
        }
    }

    // 当禁用时也取消注册
    void OnDisable()
    {
        SettingPanel.UnregisterApplyMethod(ApplySettings);
    }

    // 可选：添加退出全景场景的方法
    public void ExitPanorama()
    {
        // 清理资源
        if (panoramaRT != null)
        {
            panoramaRT.Release();
            Destroy(panoramaRT);
        }

        // 停止播放
        if (videoPlayer) videoPlayer.Stop();
        if (audioSource) audioSource.Stop();

        // 返回主场景
        SceneManager.LoadScene(SettingPanel.Instance.mainSceneName);
    }
}