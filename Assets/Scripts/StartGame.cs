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

        // 4. 流程控制与预加载
        if (GameData.Instance != null && !GameData.Instance.HasPlayedIntro)
        {
            // === 【关键修改】 ===
            // 立即开始准备第二段视频，让它在后台加载数据
            if (loopPlayer)
            {
                loopPlayer.gameObject.SetActive(true); // 必须激活才能 Prepare，但我们暂不 Play
                loopPlayer.playOnAwake = false;       // 确保 Inspector 里也是关的，防止自动播
                loopPlayer.Prepare();                 // 【核心】只预加载，不播放
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

    // ... (中间的 Help 面板逻辑保持不变) ...
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
    // ... (中间逻辑结束) ...

    void PlayIntroSequence()
    {
        // 注意：这里不要把 loopPlayer SetActive(false)，因为我们正在 Prepare 它
        // 只要 introPlayer 挡在它前面（Render Order 更高）或者 loopPlayer 还没 Play 即可

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
    // 【核心修改】零延迟切换协程
    // =========================================================
    void OnIntroFinished(VideoPlayer vp)
    {
        StartCoroutine(SwitchVideoSeamlessly());

        // UI 显示
        if (uiGroup)
        {
            uiGroup.alpha = 1f;
            uiGroup.interactable = true;
            uiGroup.blocksRaycasts = true;
        }
    }

    IEnumerator SwitchVideoSeamlessly()
    {
        // 1. 此时 loopPlayer 应该已经 Prepare 完成了
        if (loopPlayer)
        {
            // 确保物体是激活的
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true;

            // 因为已经在 Start() 里 Prepare 过了，这里调用 Play 会非常快
            loopPlayer.Play();

            // 2. 【双重保险检测】
            // 我们不检测 isPrepared (因为可能还没好)，也不只检测 isPlaying
            // 我们检测 frame > 0，这意味着 GPU 真的已经渲染出了至少一帧画面

            // 等待直到 loopPlayer 真正输出了画面
            while (loopPlayer.frame <= 0)
            {
                yield return null;
            }
        }

        // 3. 只有当新画面确实渲染出来覆盖住屏幕了，才关掉旧的
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