using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer;
    public CanvasGroup uiGroup;
    public AudioSource bgmAudioSource;

    [Header("流程设置")]
    [Tooltip("有声音播放的循环次数")]
    public int loopTimesWithSound = 5;

    [Tooltip("声音淡出需要的时间 (秒)")]
    public float audioFadeDuration = 2.0f;

    [Tooltip("UI 渐显需要的时间 (秒)")]
    public float uiFadeDuration = 1.5f;

    [Header("场景跳转")]
    public string nextSceneName = "Museum_Main";
    public Button startBtn;
    public Button quitBtn;

    // 内部变量
    private int currentLoopCount = 0;
    private bool transitionStarted = false;

    void Awake()
    {
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void Start()
    {
        if (uiGroup != null)
        {
            uiGroup.alpha = 0f;
            uiGroup.interactable = false;
            uiGroup.blocksRaycasts = false;
        }

        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        if (videoPlayer != null)
        {
            videoPlayer.isLooping = true;
            videoPlayer.loopPointReached += OnLoopPointReached;

            float initVol = (SettingPanel.Instance != null) ? SettingPanel.CurrentSettings.bgmVolume : 1.0f;
            SetVideoVolume(initVol);

            videoPlayer.Play();
        }

        if (startBtn) startBtn.onClick.AddListener(StartButton);
        if (quitBtn) quitBtn.onClick.AddListener(QuitButton);
    }

    // 【新增】每帧检查：如果设置面板打开了，禁止点后面的按钮
    void Update()
    {
        // 1. 声音淡出过程中的逻辑
        if (transitionStarted) return;

        // 2. 面板互斥逻辑：如果设置面板打开了，禁用UI组；关掉了，恢复UI组
        if (SettingPanel.Instance != null && uiGroup != null && uiGroup.alpha >= 0.9f)
        {
            // 如果面板开了 -> 禁止交互
            if (SettingPanel.Instance.isPanelActive)
            {
                if (uiGroup.interactable) uiGroup.interactable = false;
                if (uiGroup.blocksRaycasts) uiGroup.blocksRaycasts = false;
            }
            // 如果面板关了 -> 允许交互
            else
            {
                if (!uiGroup.interactable) uiGroup.interactable = true;
                if (!uiGroup.blocksRaycasts) uiGroup.blocksRaycasts = true;
            }
        }
    }

    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        if (transitionStarted) return;
        loopTimesWithSound = settings.startGameVideoLoopCount;
        SetVideoVolume(settings.bgmVolume);
        Debug.Log($"StartGame: 同步设置 - 循环: {loopTimesWithSound}, 音量: {settings.bgmVolume}");
    }

    void OnLoopPointReached(VideoPlayer vp)
    {
        if (transitionStarted) return;

        currentLoopCount++;
        if (currentLoopCount >= loopTimesWithSound)
        {
            StartTransition();
        }
    }

    void StartTransition()
    {
        transitionStarted = true;
        videoPlayer.loopPointReached -= OnLoopPointReached;
        StartCoroutine(FadeOutAudio());
        StartCoroutine(FadeInUI());
    }

    IEnumerator FadeOutAudio()
    {
        float timer = 0f;
        float startVolume = GetCurrentVideoVolume();

        while (timer < audioFadeDuration)
        {
            timer += Time.deltaTime;
            float newVolume = Mathf.Lerp(startVolume, 0f, timer / audioFadeDuration);
            SetVideoVolume(newVolume);
            yield return null;
        }

        SetVideoVolume(0f);
    }

    IEnumerator FadeInUI()
    {
        float timer = 0f;
        while (timer < uiFadeDuration)
        {
            timer += Time.deltaTime;
            if (uiGroup) uiGroup.alpha = Mathf.Lerp(0f, 1f, timer / uiFadeDuration);
            yield return null;
        }

        if (uiGroup)
        {
            uiGroup.alpha = 1f;
            uiGroup.interactable = true;
            uiGroup.blocksRaycasts = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetVideoVolume(float volume)
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
        else if (videoPlayer != null)
        {
            for (ushort i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume(i, volume);
            }
        }
    }

    float GetCurrentVideoVolume()
    {
        if (bgmAudioSource != null) return bgmAudioSource.volume;
        if (videoPlayer != null && videoPlayer.audioTrackCount > 0) return videoPlayer.GetDirectAudioVolume(0);
        return 1.0f;
    }

    void StartButton()
    {
        Time.timeScale = 1f;
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneName);
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