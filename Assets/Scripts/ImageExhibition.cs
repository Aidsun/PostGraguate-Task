using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ImageExhibition : MonoBehaviour
{
    [Header("图片展品信息")]
    public string ImageTitle;
    public Sprite ImageSprite;
    [TextArea(5, 10)] public string ImageDescriptionText;

    [Header("解说设置")]
    [Tooltip("是否启用语音解说？")]
    public bool enableVoiceover = true; // 开关
    [Tooltip("图片描述音频")]
    public AudioClip artAudioClip;

    [Header("组件设置")]
    public Renderer ContentCover;
    public Renderer outlineRenderer;
    public TMP_Text ShowTitle;

    [Header("跳转目标场景")]
    public string targetSceneName = "ImageContent";

    private void Start()
    {
        if (ShowTitle != null) ShowTitle.text = "《" + ImageTitle + "》";

        if (ContentCover != null && ImageSprite != null)
        {
            // 简单设置封面材质
            ContentCover.material.shader = Shader.Find("Unlit/Texture");
            ContentCover.material.mainTexture = ImageSprite.texture;
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (outlineRenderer != null)
            outlineRenderer.material.color = isActive ? Color.blue : Color.white;
    }

    public void StartDisplay()
    {
        // 1. 打包数据
        GameDate.ImageDate dataPackage = new GameDate.ImageDate();
        dataPackage.Title = this.ImageTitle;
        dataPackage.DescriptionText = this.ImageDescriptionText;

        // 【关键修正】这里统一使用 ImageFile，对应 GameDate 中的定义
        dataPackage.ImageFile = this.ImageSprite;

        // 音频逻辑
        dataPackage.DescriptionAudio = enableVoiceover ? this.artAudioClip : null;

        // 2. 发送数据
        GameDate.CurrentImageData = dataPackage;

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
            // 应该在从图片展示场景返回时设置

            Debug.Log($"图片展品：已保存玩家位置 {activePlayer.position}");
        }
    }
}