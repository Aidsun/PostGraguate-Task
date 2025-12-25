using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class VideoExhibition : MonoBehaviour
{
    [Header("视频展品信息")]
    public string VideoTitle;
    public VideoClip VideoFile;
    public Sprite VideoCover;
    [TextArea(5, 10)] public string VideoDescriptionText;

    [Header("解说设置")]
    [Tooltip("是否启用语音解说？")]
    public bool enableVoiceover = true; // 开关
    [Tooltip("描述音频")]
    public AudioClip artAudioClip;

    [Header("组件设置")]
    public Renderer ContentCover;
    public Renderer outlineRenderer;
    public TMP_Text ShowTitle;

    [Header("跳转设置")]
    public string targetSceneName = "VideoContent";

    private void Start()
    {
        // 设置标题
        if (ShowTitle != null) ShowTitle.text = "《" + VideoTitle + "》";

        // 设置封面
        if (ContentCover != null && VideoCover != null)
        {
            ContentCover.material.shader = Shader.Find("Unlit/Texture");
            ContentCover.material.mainTexture = VideoCover.texture;
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (outlineRenderer != null)
            outlineRenderer.material.color = isActive ? Color.yellow : Color.white;
    }

    public void StartDisplay()
    {
        // 1. 打包数据
        GameDate.VideoDate dataPackage = new GameDate.VideoDate();
        dataPackage.Title = this.VideoTitle;
        dataPackage.DescriptionText = this.VideoDescriptionText;
        dataPackage.VideoFile = this.VideoFile;

        // 【核心逻辑】如果开关打开，才传递音频；否则传 null
        dataPackage.DescriptionAudio = enableVoiceover ? this.artAudioClip : null;

        // 2. 发送数据
        GameDate.CurrentVideoDate = dataPackage;

        // 3. 保存当前位置
        SavePlayerPosition();

        // 4. 跳转场景
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }

    private void SavePlayerPosition()
    {
        SwitchViews switchScript = FindObjectOfType<SwitchViews>();
        if (switchScript != null)
        {
            Transform activePlayer = switchScript.GetActivePlayerTransform();
            GameDate.LastPlayerPosition = activePlayer.position;
            GameDate.LastPlayerRotation = activePlayer.rotation;

            // 记录进入时的视角状态
            GameDate.WasFirstPerson = switchScript.IsInFirstPerson();

            // 【注意】不要在这里设置 ShouldRestorePosition = true
            // 应该在从视频展示场景返回时设置

            Debug.Log($"视频展品：已保存玩家位置 {activePlayer.position}");
        }
    }
}