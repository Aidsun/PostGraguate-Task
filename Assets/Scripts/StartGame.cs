using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

public class StartGame : MonoBehaviour
{
    [Header("=== 视频播放器 ===")]
    public VideoPlayer introPlayer;
    public VideoPlayer loopPlayer;

    [Header("=== UI 组件 ===")]
    public CanvasGroup uiGroup;
    public GameObject helpPanel;
    public Button startBtn;
    public Button helpBtn;
    public Button quitBtn;
    public Button closeHelpBtn;

    public string nextSceneName = "Museum_Main";

    // 【新增】用来记录打开帮助面板前，哪一个视频正在播放
    private VideoPlayer pausedPlayer;

    void Start()
    {
        // 1. 初始化UI状态
        if (uiGroup) { uiGroup.alpha = 0f; uiGroup.interactable = false; uiGroup.blocksRaycasts = false; }
        if (helpPanel) helpPanel.SetActive(false);

        // 2. 绑定按钮 (修改了 Help 相关的绑定，改用专用方法)
        if (startBtn) startBtn.onClick.AddListener(OnStartGame);
        if (helpBtn) helpBtn.onClick.AddListener(OnOpenHelp);     // 改用 OnOpenHelp
        if (closeHelpBtn) closeHelpBtn.onClick.AddListener(OnCloseHelp); // 改用 OnCloseHelp
        if (quitBtn) quitBtn.onClick.AddListener(OnQuitGame);

        // 3. 音频路由
        RouteAudioToBgm(introPlayer);
        RouteAudioToBgm(loopPlayer);

        // 4. 流程控制
        if (GameData.Instance != null && !GameData.Instance.HasPlayedIntro)
        {
            PlayIntroSequence();
            GameData.Instance.HasPlayedIntro = true;
        }
        else
        {
            SkipIntroSequence();
        }
    }

    // === 新增：打开帮助面板时的逻辑 ===
    void OnOpenHelp()
    {
        PlayClick();
        if (helpPanel) helpPanel.SetActive(true);

        // 检测并暂停正在播放的视频
        if (introPlayer != null && introPlayer.isPlaying)
        {
            introPlayer.Pause();
            pausedPlayer = introPlayer; // 记住是 Intro 被暂停了
        }
        else if (loopPlayer != null && loopPlayer.isPlaying)
        {
            loopPlayer.Pause();
            pausedPlayer = loopPlayer; // 记住是 Loop 被暂停了
        }
    }

    // === 新增：关闭帮助面板时的逻辑 ===
    void OnCloseHelp()
    {
        PlayClick();
        if (helpPanel) helpPanel.SetActive(false);

        // 恢复刚才被暂停的视频
        if (pausedPlayer != null)
        {
            pausedPlayer.Play();
            pausedPlayer = null; // 清空记录
        }
    }

    void PlayIntroSequence()
    {
        if (loopPlayer) loopPlayer.gameObject.SetActive(false);
        if (introPlayer)
        {
            introPlayer.gameObject.SetActive(true);
            introPlayer.loopPointReached += OnIntroFinished;
            introPlayer.Play();
        }
        else OnIntroFinished(null);
    }

    void SkipIntroSequence()
    {
        if (introPlayer) introPlayer.gameObject.SetActive(false);
        OnIntroFinished(null);
        if (uiGroup) { uiGroup.alpha = 1f; uiGroup.interactable = true; uiGroup.blocksRaycasts = true; }
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        if (loopPlayer)
        {
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true; // 强制开启循环
            loopPlayer.Play();
        }
        if (introPlayer) { introPlayer.gameObject.SetActive(false); }
        if (uiGroup && uiGroup.alpha < 1f) StartCoroutine(FadeInUI());
    }

    void RouteAudioToBgm(VideoPlayer vp)
    {
        if (vp == null || AudioManager.Instance == null || AudioManager.Instance.BgmSource == null) return;

        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, AudioManager.Instance.BgmSource);
    }

    IEnumerator FadeInUI()
    {
        float timer = 0f;
        while (timer < 1.5f)
        {
            timer += Time.deltaTime;
            uiGroup.alpha = Mathf.Lerp(0f, 1f, timer / 1.5f);
            yield return null;
        }
        uiGroup.alpha = 1f;
        uiGroup.interactable = true;
        uiGroup.blocksRaycasts = true;
    }

    void PlayClick() { if (AudioManager.Instance) AudioManager.Instance.PlayClickSound(); }

    void OnStartGame()
    {
        // 强制恢复时间，防止意外暂停带入下一关
        Time.timeScale = 1f;
        PlayClick();
        SceneLoading.LoadLevel(nextSceneName);
    }

    void OnQuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}