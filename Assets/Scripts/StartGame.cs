using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;   // 拖入挂载 Video Player 的物体
    public CanvasGroup uiGroup;       // 拖入 UI 整体的 CanvasGroup
    public AudioSource bgmAudioSource;// 拖入播放视频声音的 AudioSource

    [Header("流程设置")]
    [Tooltip("有声音播放的循环次数")]
    public int loopTimesWithSound = 5;
    [Tooltip("声音淡出需要的时间 (秒)")]
    public float audioFadeDuration = 1.5f;
    [Tooltip("UI 渐显需要的时间 (秒)")]
    public float uiFadeDuration = 1.5f;

    [Header("场景跳转")]
    public string nextSceneName = "Museum_Main";
    public Button startBtn;
    public Button quitBtn;

    // 内部变量
    private int currentLoopCount = 0;
    private bool transitionStarted = false;

    void Start()
    {
        // 1. 初始化 UI：隐藏
        if (uiGroup != null)
        {
            uiGroup.alpha = 0f;
            uiGroup.interactable = false;
            uiGroup.blocksRaycasts = false;
        }

        // 2. 准备视频
        if (videoPlayer != null)
        {
            // 关键：这次我们需要它一直循环，不需要脚本去改 Loop 状态
            videoPlayer.isLooping = true;

            // 确保初始音量是最大的
            SetVideoVolume(1.0f);

            // 监听每次循环结束的时刻
            videoPlayer.loopPointReached += OnLoopPointReached;

            videoPlayer.Play();
        }

        // 3. 绑定按钮
        if (startBtn) startBtn.onClick.AddListener(StartButton);
        if (quitBtn) quitBtn.onClick.AddListener(QuitButton);
    }

    // 每当视频播放完一次循环，就会调用这个函数
    void OnLoopPointReached(VideoPlayer vp)
    {
        if (transitionStarted) return; // 如果已经开始淡出了，就不用管了

        currentLoopCount++; // 计数 +1
        // Debug.Log($"当前播放第 {currentLoopCount} 遍");

        // 如果播放次数达到了设定值 (比如 5次)
        if (currentLoopCount >= loopTimesWithSound)
        {
            transitionStarted = true;

            // 移除监听，后面不需要再数了
            videoPlayer.loopPointReached -= OnLoopPointReached;

            // 启动：声音淡出 + UI 渐显
            StartCoroutine(FadeOutAudio());
            StartCoroutine(FadeInUI());
        }
    }

    // --- 声音淡出协程 ---
    IEnumerator FadeOutAudio()
    {
        float timer = 0f;
        float startVolume = 1.0f;

        while (timer < audioFadeDuration)
        {
            timer += Time.deltaTime;
            // 计算当前音量：从 1 慢慢变成 0
            float newVolume = Mathf.Lerp(startVolume, 0f, timer / audioFadeDuration);
            SetVideoVolume(newVolume);
            yield return null;
        }

        // 确保最后彻底静音
        SetVideoVolume(0f);
    }

    // --- UI 渐显协程 ---
    IEnumerator FadeInUI()
    {
        float timer = 0f;
        while (timer < uiFadeDuration)
        {
            timer += Time.deltaTime;
            uiGroup.alpha = Mathf.Lerp(0f, 1f, timer / uiFadeDuration);
            yield return null;
        }

        uiGroup.alpha = 1f;
        uiGroup.interactable = true;
        uiGroup.blocksRaycasts = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- 辅助函数：统一控制音量 ---
    void SetVideoVolume(float volume)
    {
        // 情况 A: 如果您用了 AudioSource (推荐，声音控制最平滑)
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
        // 情况 B: 如果您用的是 Direct 模式 (直接输出)
        else if (videoPlayer != null)
        {
            // 尝试设置所有声道的音量
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume(i, volume);
            }
        }
    }

    void StartButton()
    {
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(nextSceneName);
        else SceneManager.LoadScene(nextSceneName);
    }

    void QuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}