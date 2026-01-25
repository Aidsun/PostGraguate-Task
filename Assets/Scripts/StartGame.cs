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

    private VideoPlayer pausedPlayer;

    void Start()
    {
        // 强制显示并解锁鼠标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 1. 初始化UI状态
        if (uiGroup) { uiGroup.alpha = 0f; uiGroup.interactable = false; uiGroup.blocksRaycasts = false; }
        if (helpPanel) helpPanel.SetActive(false);

        // 2. 绑定按钮 
        if (startBtn) startBtn.onClick.AddListener(OnStartGame);
        if (helpBtn) helpBtn.onClick.AddListener(OnOpenHelp);
        if (closeHelpBtn) closeHelpBtn.onClick.AddListener(OnCloseHelp);
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

    void Update()
    {
        // 跳过逻辑
        if (GameData.Instance != null && GameData.Instance.AllowSkipIntro)
        {
            if (introPlayer != null && introPlayer.gameObject.activeSelf && introPlayer.isPlaying)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
                {
                    Debug.Log("用户操作：跳过片头视频");
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

        if (introPlayer != null && introPlayer.isPlaying)
        {
            introPlayer.Pause();
            pausedPlayer = introPlayer;
        }
        else if (loopPlayer != null && loopPlayer.isPlaying)
        {
            loopPlayer.Pause();
            pausedPlayer = loopPlayer;
        }
    }

    void OnCloseHelp()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
        if (helpPanel) helpPanel.SetActive(false);

        if (pausedPlayer != null)
        {
            pausedPlayer.Play();
            pausedPlayer = null;
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
    }

    // =========================================================
    // 【核心修改】视频切换逻辑
    // =========================================================
    void OnIntroFinished(VideoPlayer vp)
    {
        // 启动协程来平滑切换
        StartCoroutine(SwitchVideoSmoothly());

        // UI 直接显示
        if (uiGroup)
        {
            uiGroup.alpha = 1f;
            uiGroup.interactable = true;
            uiGroup.blocksRaycasts = true;
        }
    }

    // 新增：平滑切换协程
    IEnumerator SwitchVideoSmoothly()
    {
        // 1. 先激活并播放循环视频 (此时 introPlayer 还没关，挡在后面)
        if (loopPlayer)
        {
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true;
            loopPlayer.Play();
        }

        // 2. 关键点：等待几帧，直到 loopPlayer 真正准备好并输出了画面
        if (loopPlayer)
        {
            // 等待直到状态变为 Playing
            while (!loopPlayer.isPlaying)
            {
                yield return null;
            }
            // 【双重保险】额外多等 2 帧，确保画面数据已经从 GPU 渲染到了屏幕上
            // 这一步能彻底消灭“闪烁”
            yield return null;
            yield return null;
        }

        // 3. 新视频已经盖在上面了，现在安全关闭旧视频
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

    void OnStartGame()
    {
        Time.timeScale = 1f;
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
        SceneLoading.LoadLevel(nextSceneName);
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