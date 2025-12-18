using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.SceneManagement;

public class VideoDisplayController : MonoBehaviour
{
    [Header("核心组件")]
    public VideoPlayer videoPlayer; // 拖入场景里的 Video Player
    public RawImage displayScreen;  // 拖入 UI 上的 Raw Image (显示画面的)

    [Header("UI 信息绑定")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("控制按钮")]
    public Button playPauseButton;
    public TMP_Text playPauseBtnText;
    public Button exitButton;

    [Header("返回设置")]
    public string returnSceneName = "Museum_Main";

    void Start()
    {
        // 1. 读取全局数据
        var data = GameDate.CurrentVideoDate;

        if (data != null)
        {
            if (titleText) titleText.text = data.Title;
            if (descriptionText) descriptionText.text = data.DescriptionText;

            if (videoPlayer && data.VideoFile)
            {
                // 设置视频源
                videoPlayer.clip = data.VideoFile;

                // 准备并播放 (Render Texture 模式)
                videoPlayer.Play();
            }
        }
        else
        {
            Debug.LogError("【错误】没有读取到视频数据，请从浏览馆入口进入！");
        }

        // 2. 绑定按钮事件
        if (exitButton) exitButton.onClick.AddListener(OnExit);
        if (playPauseButton) playPauseButton.onClick.AddListener(OnPlayPause);

        // 3. 解锁鼠标 (非常重要，否则你看得到光标但点不了)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnPlayPause()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            if (playPauseBtnText) playPauseBtnText.text = "播放";
        }
        else
        {
            videoPlayer.Play();
            if (playPauseBtnText) playPauseBtnText.text = "暂停";
        }
    }

    void OnExit()
    {
        // 停止视频
        if (videoPlayer) videoPlayer.Stop();

        // 返回大厅 (使用加载器)
        if (System.Type.GetType("SceneLoding") != null)
        {
            SceneLoding.LoadLevel(returnSceneName);
        }
        else
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }
}