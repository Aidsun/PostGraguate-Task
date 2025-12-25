using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PanoramaDisplayController : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;
    public Material skyboxMaterial; // 拖入 Skybox_Panorama 材质球

    [Header("UI 与 音频")]
    public TMP_Text titleText;
    public Button exitButton;       // 左上角的返回按钮
    public AudioSource audioSource; // 拖入场景中的 AudioSource (用于播放解说)

    [Header("设置")]
    public string returnSceneName = "Museum_Main";

    // 内部变量
    private RenderTexture panoramaRT;
    private float currentVideoVolume = 1.0f;
    private float currentDescriptionVolume = 1.0f;
    private bool isPausedByPanel = false;

    void Awake()
    {
        // 注册到设置面板
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        // 1. 注销设置监听
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }

        // 2. 释放 RenderTexture 内存
        if (panoramaRT != null)
        {
            panoramaRT.Release();
            Destroy(panoramaRT);
            panoramaRT = null;
        }
    }

    void Start()
    {
        // 1. 应用初始设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        // 2. 获取全景数据
        var data = GameDate.CurrentPanoramaDate;

        if (data != null)
        {
            // 设置标题
            if (titleText) titleText.text = "《" + data.Title + "》";

            // 设置全景视频
            // 【关键修正】这里改为大写的 PanoramaFile
            if (videoPlayer && data.PanoramaFile)
            {
                videoPlayer.clip = data.PanoramaFile;

                // 创建高分辨率渲染纹理 (4K)
                panoramaRT = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = panoramaRT;

                // 将纹理赋给天空盒材质
                if (skyboxMaterial != null)
                {
                    skyboxMaterial.SetTexture("_MainTex", panoramaRT);
                    RenderSettings.skybox = skyboxMaterial;
                }

                // 准备并播放
                videoPlayer.Prepare();
                videoPlayer.Play();

                // 确保音量正确
                SetVideoVolume(currentVideoVolume);
            }

            // 播放解说音频
            if (audioSource != null && data.DescriptionAudio != null)
            {
                audioSource.clip = data.DescriptionAudio;
                audioSource.volume = currentDescriptionVolume;
                audioSource.Play();
            }
        }
        else
        {
            Debug.LogError("未读取到全景数据！请从博物馆场景进入。");
        }

        // 3. 绑定返回按钮
        if (exitButton)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        // 4. 解锁鼠标 (全景模式下通常需要用鼠标拖动视角，但在Web/PC端可能不同，这里先解锁以便点击UI)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        currentVideoVolume = settings.videoVolume;
        currentDescriptionVolume = settings.descriptionVolume;

        // 实时更新解说音量
        if (audioSource != null)
        {
            audioSource.volume = currentDescriptionVolume;
        }

        // 实时更新视频音量
        SetVideoVolume(currentVideoVolume);

        Debug.Log($"全景控制器: 同步音量 - 视频: {currentVideoVolume}, 解说: {currentDescriptionVolume}");
    }

    void Update()
    {
        // 检测面板状态，控制暂停/继续
        if (SettingPanel.Instance != null)
        {
            if (SettingPanel.Instance.isPanelActive)
            {
                // 面板打开时，如果正在播放，则暂停
                if (!isPausedByPanel)
                {
                    PauseAll(true);
                    isPausedByPanel = true;
                }
            }
            else
            {
                // 面板关闭时，恢复播放
                if (isPausedByPanel)
                {
                    PauseAll(false);
                    isPausedByPanel = false;
                }
            }
        }

        // 额外的鼠标交互逻辑可以写在这里（比如按住左键旋转视角），
        // 但通常 Skybox shader 或者单独的 Camera Controller 会处理这个。
    }

    // 暂停/恢复所有媒体
    void PauseAll(bool shouldPause)
    {
        if (shouldPause)
        {
            if (videoPlayer && videoPlayer.isPlaying) videoPlayer.Pause();
            if (audioSource && audioSource.isPlaying) audioSource.Pause();
        }
        else
        {
            if (videoPlayer && !videoPlayer.isPlaying) videoPlayer.Play();
            // 只有当音频没播完时才恢复
            if (audioSource && !audioSource.isPlaying && audioSource.time > 0 && audioSource.time < audioSource.clip.length)
                audioSource.UnPause();
        }
    }

    void SetVideoVolume(float volume)
    {
        if (videoPlayer != null)
        {
            // 尝试设置所有轨道的音量
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume(i, volume);
            }
        }
    }

    void OnExitButtonClicked()
    {
        // 停止播放
        if (videoPlayer) videoPlayer.Stop();
        if (audioSource) audioSource.Stop();

        // 标记需要恢复位置
        GameDate.ShouldRestorePosition = true;

        // 安全跳转
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(returnSceneName);
        else
            SceneManager.LoadScene(returnSceneName);
    }
}