using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 必须引入
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

    // 【新增】加载遮罩 (防止连点)
    public GameObject loadingMask;

    public string nextSceneName = "Museum_Main";

    private VideoPlayer pausedPlayer;
    private bool isStarting = false; // 防止重复点击

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (uiGroup) { uiGroup.alpha = 0f; uiGroup.interactable = false; uiGroup.blocksRaycasts = false; }
        if (helpPanel) helpPanel.SetActive(false);
        if (loadingMask) loadingMask.SetActive(false); // 隐藏遮罩

        if (startBtn) startBtn.onClick.AddListener(OnStartGame);
        if (helpBtn) helpBtn.onClick.AddListener(OnOpenHelp);
        if (closeHelpBtn) closeHelpBtn.onClick.AddListener(OnCloseHelp);
        if (quitBtn) quitBtn.onClick.AddListener(OnQuitGame);

        RouteAudioToBgm(introPlayer);
        RouteAudioToBgm(loopPlayer);

        if (GameData.Instance != null && !GameData.Instance.HasPlayedIntro)
        {
            if (loopPlayer)
            {
                loopPlayer.gameObject.SetActive(true);
                loopPlayer.playOnAwake = false;
                loopPlayer.Prepare();
            }
            PlayIntroSequence();
            GameData.Instance.HasPlayedIntro = true;
        }
        else
        {
            SkipIntroSequence();
        }
    }

    void Update()
    {
        if (GameData.Instance != null && GameData.Instance.AllowSkipIntro)
        {
            if (introPlayer != null && introPlayer.gameObject.activeSelf && introPlayer.isPlaying)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
                {
                    introPlayer.loopPointReached -= OnIntroFinished;
                    OnIntroFinished(introPlayer);
                }
            }
        }
    }

    void OnOpenHelp()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
        if (helpPanel) helpPanel.SetActive(true);
        if (introPlayer != null && introPlayer.isPlaying) { introPlayer.Pause(); pausedPlayer = introPlayer; }
        else if (loopPlayer != null && loopPlayer.isPlaying) { loopPlayer.Pause(); pausedPlayer = loopPlayer; }
    }

    void OnCloseHelp()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
        if (helpPanel) helpPanel.SetActive(false);
        if (pausedPlayer != null) { pausedPlayer.Play(); pausedPlayer = null; }
    }

    void PlayIntroSequence()
    {
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
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(SwitchVideoSeamlessly());
        if (uiGroup)
        {
            uiGroup.alpha = 1f;
            uiGroup.interactable = true;
            uiGroup.blocksRaycasts = true;
        }
    }

    IEnumerator SwitchVideoSeamlessly()
    {
        if (loopPlayer)
        {
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true;
            loopPlayer.Play();
            while (loopPlayer.frame <= 0) yield return null;
        }
        if (introPlayer)
        {
            introPlayer.Stop();
            introPlayer.gameObject.SetActive(false);
        }
    }

    void RouteAudioToBgm(VideoPlayer vp)
    {
        if (vp == null || AudioManager.Instance == null || AudioManager.Instance.BgmSource == null) return;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, AudioManager.Instance.BgmSource);
    }

    // =========================================================
    // 【核心优化】点击开始按钮后的逻辑
    // =========================================================
    void OnStartGame()
    {
        if (isStarting) return; // 防止重复点击
        isStarting = true;

        Time.timeScale = 1f;
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();

        // 1. 显示一个简单的遮罩 (可选，比如转圈圈)，防止用户觉得没反应
        if (loadingMask) loadingMask.SetActive(true);

        // 2. 开启协程，平滑切换到 LoadingScene
        StartCoroutine(TransitionToLoading());
    }

    IEnumerator TransitionToLoading()
    {
        // 告诉 SceneLoading 下一站是哪里
        SceneLoading.SceneToLoad = nextSceneName;

        // 3. 异步加载 "LoadingScene"
        // 这一步非常快，因为 LoadingScene 只有几张图片，几乎瞬间完成
        AsyncOperation op = SceneManager.LoadSceneAsync("LoadingScene");

        // 禁止自动跳转，直到加载完成 (虽然这里很快，但保持习惯)
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 4. 允许跳转，瞬间进入加载界面
        op.allowSceneActivation = true;

        // 之后的事情就交给 SceneLoading.cs 去处理那个巨大的 Museum_Main 了
    }

    void OnQuitGame()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}