using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartGame : MonoBehaviour
{
    [Header("=== 核心组件 ===")]
    public VideoPlayer videoPlayer;
    public CanvasGroup uiGroup;
    public AudioSource bgmAudioSource;

    [Header("=== 帮助面板 ===")]
    public GameObject helpPanel;
    public Button closeHelpBtn;

    [Header("=== 按钮绑定 ===")]
    public Button startBtn;
    public Button helpBtn;
    public Button quitBtn;

    [Header("=== 参数设置 ===")]
    public string nextSceneName = "Museum_Main";
    public float uiFadeDuration = 1.5f;

    private bool isIntroPlaying = false;

    private void Start()
    {
        if (uiGroup != null)
        {
            uiGroup.alpha = 0f;
            uiGroup.interactable = false;
            uiGroup.blocksRaycasts = false;
        }
        if (helpPanel != null) helpPanel.SetActive(false);

        if (startBtn) startBtn.onClick.AddListener(OnStartGame);
        if (helpBtn) helpBtn.onClick.AddListener(OnOpenHelp);
        if (quitBtn) quitBtn.onClick.AddListener(OnQuitGame);
        if (closeHelpBtn) closeHelpBtn.onClick.AddListener(OnCloseHelp);

        PlayBackgroundVideo();
    }

    private void Update()
    {
        // 【新增】点击鼠标左键跳过介绍视频
        if (isIntroPlaying && Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }

    private void PlayBackgroundVideo()
    {
        if (videoPlayer == null || GameData.Instance == null) return;

        UpdateVideoVolume();

        if (GameData.Instance.HasPlayedIntro || GameData.Instance.StartIntroVideo == null)
        {
            PlayLoopVideo();
        }
        else
        {
            isIntroPlaying = true;
            videoPlayer.clip = GameData.Instance.StartIntroVideo;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnIntroFinished;
            videoPlayer.Play();

            GameData.Instance.HasPlayedIntro = true;
        }
    }

    private void OnIntroFinished(VideoPlayer vp)
    {
        vp.loopPointReached -= OnIntroFinished;
        PlayLoopVideo();
    }

    // 【新增】跳过逻辑
    private void SkipIntro()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnIntroFinished; // 移除监听
            PlayLoopVideo(); // 强行进入下一阶段
        }
    }

    private void PlayLoopVideo()
    {
        isIntroPlaying = false;

        if (GameData.Instance.StartLoopVideo != null)
        {
            videoPlayer.clip = GameData.Instance.StartLoopVideo;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        StartCoroutine(FadeInUI());
    }

    private IEnumerator FadeInUI()
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

    private void UpdateVideoVolume()
    {
        float vol = GameData.Instance.BgmVolume;
        if (bgmAudioSource != null) bgmAudioSource.volume = vol;
        else if (videoPlayer != null) videoPlayer.SetDirectAudioVolume(0, vol);
    }

    void OnStartGame()
    {
        SceneLoading.LoadLevel(nextSceneName);
    }

    // 【修复】打开帮助时暂停视频和声音
    void OnOpenHelp()
    {
        if (helpPanel) helpPanel.SetActive(true);
        if (videoPlayer) videoPlayer.Pause();
        if (bgmAudioSource) bgmAudioSource.Pause();
    }

    // 【修复】关闭帮助时恢复
    void OnCloseHelp()
    {
        if (helpPanel) helpPanel.SetActive(false);
        if (videoPlayer) videoPlayer.Play();
        if (bgmAudioSource) bgmAudioSource.UnPause();
    }

    void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}