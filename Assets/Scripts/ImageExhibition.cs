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
    public bool enableVoiceover = true;
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
        GameDate.ImageDate dataPackage = new GameDate.ImageDate();
        dataPackage.Title = this.ImageTitle;
        dataPackage.DescriptionText = this.ImageDescriptionText;
        dataPackage.ImageFile = this.ImageSprite;

        // 【核心逻辑】如果开关打开，才传递音频；否则传 null
        dataPackage.DescriptionAudio = enableVoiceover ? this.artAudioClip : null;

        GameDate.CurrentImageData = dataPackage;

        // --- 保存位置逻辑 (保持不变) ---
        SwitchViews switchScript = FindObjectOfType<SwitchViews>();
        if (switchScript != null)
        {
            Transform activePlayer = switchScript.GetActivePlayerTransform();
            GameDate.LastPlayerPosition = activePlayer.position;
            GameDate.LastPlayerRotation = activePlayer.rotation;
            GameDate.ShouldRestorePosition = true;
            GameDate.WasFirstPerson = switchScript.IsInFirstPerson();
        }
        // -----------------------------

        if (FindObjectOfType<SceneLoding>() != null || System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }
}