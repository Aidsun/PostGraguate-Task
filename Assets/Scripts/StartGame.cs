// 文件：StartGame.cs
// 模块：场景 / 开始菜单
// 说明：该脚本管理游戏开始界面（StartGame场景）的逻辑，包括开场视频的播放、跳过、帮助面板、
//      开始游戏、退出游戏等。它处理两个视频播放器（introPlayer播放一次，loopPlayer循环播放背景），
//      并在开场视频结束后显示UI。当玩家点击“开始游戏”时，它通过SceneLoading切换到主馆场景。
// 特性：使用VideoPlayer组件播放视频，通过协程实现无缝切换，处理音频路由到AudioManager的BgmSource，
//      通过CanvasGroup控制UI淡入，使用条件编译处理退出游戏时的编辑器/构建差异。

using UnityEngine;
using UnityEngine.Video;          // 视频播放器
using UnityEngine.UI;             // UI按钮、CanvasGroup等
using UnityEngine.SceneManagement; // 场景管理
using System.Collections;         // 协程

public class StartGame : MonoBehaviour
{
    [Header("=== 视频播放器 ===")]
    public VideoPlayer introPlayer;   // 播放开场视频（只播放一次）
    public VideoPlayer loopPlayer;    // 播放循环背景视频（开场结束后循环播放）

    [Header("=== UI 组件 ===")]
    public CanvasGroup uiGroup;       // 控制主菜单UI的CanvasGroup（用于淡入、交互）
    public GameObject helpPanel;      // 帮助面板根物体
    public Button startBtn;           // 开始游戏按钮
    public Button helpBtn;            // 打开帮助按钮
    public Button quitBtn;            // 退出游戏按钮
    public Button closeHelpBtn;       // 关闭帮助按钮

    // 【新增】加载遮罩 (防止连点)
    public GameObject loadingMask;    // 点击开始游戏后显示的加载遮罩（如转圈圈），防止用户重复点击

    public string nextSceneName = "Museum_Main";   // 点击开始后要加载的目标场景

    private VideoPlayer pausedPlayer; // 记录因打开帮助面板而暂停的视频播放器（用于恢复）
    private bool isStarting = false;  // 防止重复点击开始游戏

    void Start()
    {
        // 初始设置鼠标可见且不锁定（开始菜单需要鼠标操作）
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 初始化UI状态：主菜单UI完全透明且不可交互，帮助面板隐藏，加载遮罩隐藏
        if (uiGroup)
        {
            uiGroup.alpha = 0f;
            uiGroup.interactable = false;
            uiGroup.blocksRaycasts = false;
        }
        if (helpPanel) helpPanel.SetActive(false);
        if (loadingMask) loadingMask.SetActive(false);

        // 绑定按钮点击事件
        if (startBtn) startBtn.onClick.AddListener(OnStartGame);
        if (helpBtn) helpBtn.onClick.AddListener(OnOpenHelp);
        if (closeHelpBtn) closeHelpBtn.onClick.AddListener(OnCloseHelp);
        if (quitBtn) quitBtn.onClick.AddListener(OnQuitGame);

        // 将视频的音频路由到AudioManager的背景音乐源（BgmSource）
        RouteAudioToBgm(introPlayer);
        RouteAudioToBgm(loopPlayer);

        // 根据是否已播放过开场动画决定播放逻辑
        if (GameData.Instance != null && !GameData.Instance.HasPlayedIntro)
        {
            // 首次启动，准备循环播放器（但不播放），然后播放开场视频
            if (loopPlayer)
            {
                loopPlayer.gameObject.SetActive(true);
                loopPlayer.playOnAwake = false; // 确保不会自动播放
                loopPlayer.Prepare();            // 预准备视频，以便后续无缝切换
            }
            PlayIntroSequence();                 // 播放开场视频
            GameData.Instance.HasPlayedIntro = true; // 标记已播放
        }
        else
        {
            // 非首次启动，跳过开场视频，直接显示UI并播放循环视频
            SkipIntroSequence();
        }
    }

    void Update()
    {
        // 如果允许跳过开场动画，且开场视频正在播放，则监听鼠标左键或F键，按下时跳过
        if (GameData.Instance != null && GameData.Instance.AllowSkipIntro)
        {
            if (introPlayer != null && introPlayer.gameObject.activeSelf && introPlayer.isPlaying)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.F))
                {
                    // 移除已完成事件监听（避免重复调用），然后手动触发结束逻辑
                    introPlayer.loopPointReached -= OnIntroFinished;
                    OnIntroFinished(introPlayer);
                }
            }
        }
    }

    /// <summary>
    /// 打开帮助面板，暂停当前视频，并记录暂停的播放器。
    /// </summary>
    void OnOpenHelp()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound(); // 播放点击音效
        if (helpPanel) helpPanel.SetActive(true);                           // 显示帮助面板

        // 如果开场视频正在播放，暂停它；否则如果循环视频正在播放，暂停它
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

    /// <summary>
    /// 关闭帮助面板，恢复之前暂停的视频。
    /// </summary>
    void OnCloseHelp()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound(); // 播放点击音效
        if (helpPanel) helpPanel.SetActive(false);                         // 隐藏帮助面板
        if (pausedPlayer != null)
        {
            pausedPlayer.Play();   // 恢复视频播放
            pausedPlayer = null;    // 清除记录
        }
    }

    /// <summary>
    /// 播放开场视频序列，监听播放完成事件。
    /// </summary>
    void PlayIntroSequence()
    {
        if (introPlayer)
        {
            introPlayer.gameObject.SetActive(true);
            introPlayer.loopPointReached += OnIntroFinished; // 订阅播放完成事件
            introPlayer.Play();
        }
        else
        {
            // 如果没有introPlayer，直接触发完成逻辑
            OnIntroFinished(null);
        }
    }

    /// <summary>
    /// 跳过开场视频，直接显示UI并播放循环视频。
    /// </summary>
    void SkipIntroSequence()
    {
        if (introPlayer) introPlayer.gameObject.SetActive(false); // 隐藏introPlayer
        OnIntroFinished(null);                                     // 直接调用完成逻辑
    }

    /// <summary>
    /// 开场视频播放完成或跳过时调用，负责无缝切换到循环视频，并显示UI。
    /// </summary>
    /// <param name="vp">触发事件的VideoPlayer（可能为null）</param>
    void OnIntroFinished(VideoPlayer vp)
    {
        // 启动协程，无缝切换到循环视频
        StartCoroutine(SwitchVideoSeamlessly());
        // 显示UI（淡入效果通过CanvasGroup的alpha实现）
        if (uiGroup)
        {
            uiGroup.alpha = 1f;
            uiGroup.interactable = true;
            uiGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 协程：无缝切换视频。先启动循环视频并等待其第一帧就绪，然后停止并隐藏开场视频。
    /// </summary>
    IEnumerator SwitchVideoSeamlessly()
    {
        if (loopPlayer)
        {
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true;   // 设置为循环播放
            loopPlayer.Play();
            // 等待循环视频至少有一帧已经渲染，确保画面不黑屏
            while (loopPlayer.frame <= 0) yield return null;
        }
        if (introPlayer)
        {
            introPlayer.Stop();                // 停止开场视频
            introPlayer.gameObject.SetActive(false); // 隐藏
        }
    }

    /// <summary>
    /// 将VideoPlayer的音频输出路由到AudioManager的背景音乐源（BgmSource）。
    /// </summary>
    /// <param name="vp">要配置的VideoPlayer</param>
    void RouteAudioToBgm(VideoPlayer vp)
    {
        if (vp == null || AudioManager.Instance == null || AudioManager.Instance.BgmSource == null) return;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;      // 输出模式：AudioSource
        vp.EnableAudioTrack(0, true);                               // 启用音轨0
        vp.SetTargetAudioSource(0, AudioManager.Instance.BgmSource); // 指定目标音频源
    }

    // =========================================================
    // 【核心优化】点击开始按钮后的逻辑
    // =========================================================
    /// <summary>
    /// 开始游戏按钮点击处理：防止连点，播放点击音效，显示加载遮罩，然后通过协程切换到LoadingScene。
    /// </summary>
    void OnStartGame()
    {
        if (isStarting) return; // 防止重复点击
        isStarting = true;

        Time.timeScale = 1f;    // 确保时间缩放正常（某些场景可能暂停过）
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound(); // 播放点击音效

        // 显示加载遮罩，防止用户觉得没反应
        if (loadingMask) loadingMask.SetActive(true);

        // 开启协程，平滑切换到 LoadingScene
        StartCoroutine(TransitionToLoading());
    }

    /// <summary>
    /// 协程：切换到LoadingScene，并告诉SceneLoading下一站的目标场景。
    /// </summary>
    IEnumerator TransitionToLoading()
    {
        // 设置SceneLoading的静态字段，指定最终要加载的场景
        SceneLoading.SceneToLoad = nextSceneName;

        // 异步加载 "LoadingScene"（这是一个非常轻量的场景）
        AsyncOperation op = SceneManager.LoadSceneAsync("LoadingScene");

        // 禁止自动跳转，直到加载完成
        op.allowSceneActivation = false;

        // 等待加载进度达到0.9（即加载完成）
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 允许跳转，瞬间进入加载界面
        op.allowSceneActivation = true;

        // 之后的事情就交给 SceneLoading.cs 去处理那个巨大的 Museum_Main 了
    }

    /// <summary>
    /// 退出游戏按钮处理：播放点击音效，并在编辑器下停止播放，在构建下退出应用。
    /// </summary>
    void OnQuitGame()
    {
        if (AudioManager.Instance) AudioManager.Instance.PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器模式停止运行
#else
        Application.Quit();                               // 构建模式退出应用
#endif
    }
}