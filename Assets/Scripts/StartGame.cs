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

    // 用来记录打开帮助面板前，哪一个视频正在播放
    private VideoPlayer pausedPlayer;

    void Start()
    {
        // ============================================
        // 强制显示并解锁鼠标
        // ============================================
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // ============================================

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
            // 注意：HasPlayedIntro = true 放在这里意味着只要开始播放了就算看过
            // 如果你希望只有完整看完才算，可以移到 OnIntroFinished 里
            GameData.Instance.HasPlayedIntro = true;
        }
        else
        {
            SkipIntroSequence();
        }
    }

    // 【新增】每帧检测是否需要跳过视频
    void Update()
    {
        // 1. 检查 GameData 设置是否允许跳过
        if (GameData.Instance != null && GameData.Instance.AllowSkipIntro)
        {
            // 2. 检查是否正在播放 Intro 视频 (introPlayer 激活且正在播放)
            if (introPlayer != null && introPlayer.gameObject.activeSelf && introPlayer.isPlaying)
            {
                // 3. 检测输入：鼠标左键 (0) 或 E键
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("用户操作：跳过片头视频");
                    // 移除事件监听，防止逻辑重复执行 (虽然 SetActive false 也会阻止)
                    introPlayer.loopPointReached -= OnIntroFinished;
                    // 手动调用结束逻辑
                    OnIntroFinished(introPlayer);
                }
            }
        }
    }

    // === 打开帮助面板时的逻辑 ===
    void OnOpenHelp()
    {
        PlayClick();
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

    // === 关闭帮助面板时的逻辑 ===
    void OnCloseHelp()
    {
        PlayClick();
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
        // 如果是直接跳过（比如第二次进入游戏），UI直接显示，不需要渐变
        if (uiGroup) { uiGroup.alpha = 1f; uiGroup.interactable = true; uiGroup.blocksRaycasts = true; }
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        // 开启循环背景视频
        if (loopPlayer)
        {
            loopPlayer.gameObject.SetActive(true);
            loopPlayer.isLooping = true;
            loopPlayer.Play();
        }
        // 关闭片头视频
        if (introPlayer)
        {
            introPlayer.Stop(); // 确保停止
            introPlayer.gameObject.SetActive(false);
        }

        // 淡入 UI
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