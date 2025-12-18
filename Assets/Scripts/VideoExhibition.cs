using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class VideoExhibition : MonoBehaviour
{
    [Header("视频展品信息")]

    [Tooltip("视频标题")]
    public string VideoTitle;
    [Tooltip("视频文件")]
    public VideoClip VideoFile;
    [Tooltip("视频封面")]
    public Sprite VideoCover; // 这里保持用 Sprite，方便您直接拖拽
    [Tooltip("视频描述")]
    [TextArea(5, 10)] public string VideoDescriptionText;

    [Header("组件设置")]
    [Tooltip("展示封面组件 (记得用 Quad 模型!)")]
    public Renderer ContentCover; // 【新增】拖入那个显示封面的 Quad 物体
    [Tooltip("高亮组件")]
    public Renderer outlineRenderer;
    [Tooltip("标题显示组件")]
    public TMP_Text ShowTitle;


    [Header("跳转设置")]
    [Tooltip("跳转目标场景名称")]
    public string targetSceneName = "VideoContent";

    private void Start()
    {
        // 1. 初始化标题
        if (ShowTitle != null)
        {
            ShowTitle.text = "《" + VideoTitle + "》";
        }
        else
        {
            Debug.LogWarning($"视频 {VideoTitle} 的标题组件未绑定。");
        }

        // 2. 初始化高亮组件检测
        if (outlineRenderer == null)
        {
            Debug.LogWarning($"视频 {VideoTitle} 的高亮组件未绑定。");
        }

        // 3. 【核心】自动设置封面 (复用图片的成功逻辑)
        if (ContentCover != null && VideoCover != null)
        {
            // A. 先强制换 Shader (无光照模式，防止反光或透明)
            ContentCover.material.shader = Shader.Find("Unlit/Texture");
            // B. 再赋贴图
            ContentCover.material.mainTexture = VideoCover.texture;
        }
        else
        {
            Debug.LogWarning($"视频 {VideoTitle} 的封面组件或封面图片未设置。");
        }
    }

    // 设置高亮
    public void SetHighlight(bool isActive)
    {
        if (outlineRenderer != null)
            outlineRenderer.material.color = isActive ? Color.yellow : Color.white;
    }

    // --- 【新增】开始播放逻辑 ---
    public void StartDisplay()
    {
        // 1. 打包视频数据
        GameDate.VideoDate dataPackage = new GameDate.VideoDate();
        dataPackage.Title = this.VideoTitle;
        dataPackage.DescriptionText = this.VideoDescriptionText; // 注意变量名对应
        dataPackage.VideoFile = this.VideoFile;

        // 发送给全局
        GameDate.CurrentVideoDate = dataPackage;

        // 2. 【核心复用】保存玩家真实位置 (存儿子逻辑)
        SavePlayerPosition();

        // 3. 跳转 (使用 Loading)
        if (FindObjectOfType<SceneLoding>() != null || System.Type.GetType("SceneLoding") != null)
        {
            SceneLoding.LoadLevel(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // 封装的保存位置方法
    private void SavePlayerPosition()
    {
        SwitchViews switchScript = FindObjectOfType<SwitchViews>();
        if (switchScript != null)
        {
            Transform activePlayer = switchScript.GetActivePlayerTransform();
            GameDate.LastPlayerPosition = activePlayer.position;
            GameDate.LastPlayerRotation = activePlayer.rotation;
            GameDate.ShouldRestorePosition = true;
            GameDate.WasFirstPerson = switchScript.IsInFirstPerson();

            Debug.Log($"[视频存档] 位置: {GameDate.LastPlayerPosition}");
        }
        else
        {
            Debug.LogError("未找到 SwitchViews，无法保存位置！");
        }
    }
}