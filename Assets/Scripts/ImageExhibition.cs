using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ImageExhibition : MonoBehaviour
{
    [Header("图片展品信息")]

    [Tooltip("图片标题")]
    public string ImageTitle;
    [Tooltip("图片文件")]
    public Sprite ImageSprite;
    [Tooltip("图片描述文本")]
    [TextArea(5, 10)] public string ImageDescriptionText;
    [Tooltip("图片描述音频")]
    public AudioClip artAudioClip;

    [Header("组件设置")]
    [Tooltip("展示封面组件")]
    public Renderer ContentCover;
    [Tooltip("高亮组件")]
    public Renderer outlineRenderer;
    [Tooltip("标题显示组件")]
    public TMP_Text ShowTitle;

    [Header("跳转目标场景")]
    [Tooltip("图片场景名称")]
    public string targetSceneName = "ImageContent";

    private void Start()
    {
        //检测3D标题组件
        if (ShowTitle != null)
        {
            //展示标题内容
            ShowTitle.text = "《"+ImageTitle+"》";
        }
        else
        {
            Debug.LogWarning($"{ImageTitle}的展示标题ShowTitle组件未绑定，无法显示标题。");
        }
        //检测封面组件
        if(ContentCover != null)
        {
            //设置渲染方式
            ContentCover.material.shader = Shader.Find("Unlit/Texture");
            //设置封面贴图
            ContentCover.material.mainTexture = ImageSprite.texture;
        }
        else
        {
            Debug.LogWarning($"{ImageTitle}的封面组件ContentCover未绑定，无法显示封面。");
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
        dataPackage.ImageShow = this.ImageSprite;
        dataPackage.DescriptionAudio = this.artAudioClip;
        GameDate.CurrentImageData = dataPackage;

        // ------------------ 【核心修改：存子节点的位置】 ------------------

        // 找到 SwitchViews 脚本 (全场景搜索，最保险的方式)
        SwitchViews switchScript = FindObjectOfType<SwitchViews>();

        if (switchScript != null)
        {
            // 【关键】获取真正移动的那个子物体的 Transform
            Transform activePlayer = switchScript.GetActivePlayerTransform();

            // 保存它的世界坐标 (这才是对的！)
            GameDate.LastPlayerPosition = activePlayer.position;
            GameDate.LastPlayerRotation = activePlayer.rotation;
            GameDate.ShouldRestorePosition = true;

            // 保存视角状态
            GameDate.WasFirstPerson = switchScript.IsInFirstPerson();
            Debug.Log($"【存档成功!】,保存真实坐标: {GameDate.LastPlayerPosition}，保存视角: {(GameDate.WasFirstPerson ? "第一人称" : "第三人称")}");
        }
        else
        {
            Debug.LogError("【严重错误】找不到 SwitchViews 脚本！无法确认玩家位置！");
        }
        // -----------------------------------------------------------

        // 2. 跳转场景
        // 使用加载器跳转
        if (FindObjectOfType<SceneLoding>() != null || System.Type.GetType("SceneLoding") != null)
        {
            SceneLoding.LoadLevel(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}