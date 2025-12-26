using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class VideoExhibition : MonoBehaviour
{
    [Header("展品数据")]
    public string Title;
    public VideoClip VideoContent;
    public Sprite CoverImage; // 视频封面图
    [TextArea] public string Description;

    [Header("解说设置")]
    public bool EnableVoice = true;
    public AudioClip VoiceClip;

    [Header("组件绑定")]
    public Renderer CoverRenderer;    // 用于显示封面的3D物体
    public Renderer OutlineRenderer;  // 用于显示高亮边框的物体
    public TMP_Text TitleLabel;       // 显示标题的3D文本

    [Header("目标场景")]
    public string TargetScene = "VideoContent";

    void Start()
    {
        // 初始化显示
        if (TitleLabel) TitleLabel.text = Title;

        if (CoverRenderer && CoverImage)
        {
            // 假设封面材质 shader 是 Unlit/Texture 或 Standard
            CoverRenderer.material.mainTexture = CoverImage.texture;
        }
    }

    // 由 PlayerInteraction 反射调用
    public void SetHighlight(bool active)
    {
        if (OutlineRenderer && GameData.Instance)
        {
            OutlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    // 由 PlayerInteraction 反射调用
    public void StartDisplay()
    {
        // 1. 保存玩家当前位置和视角
        SavePlayerState();

        // 2. 打包数据发送给 GameData
        GameData.VideoPacket packet = new GameData.VideoPacket();
        packet.Title = this.Title;
        packet.VideoContent = this.VideoContent;
        packet.Description = this.Description;
        packet.AutoPlayVoice = this.EnableVoice;
        packet.VoiceClip = this.VoiceClip;

        GameData.CurrentVideo = packet; // 存入全局静态变量

        // 3. 跳转到视频展示场景
        SceneLoading.LoadLevel(TargetScene);
    }

    private void SavePlayerState()
    {
        // 查找视角控制脚本来获取准确的位置
        SwitchViews sv = FindObjectOfType<SwitchViews>();
        if (sv && GameData.Instance)
        {
            Transform p = sv.GetActivePlayerTransform();
            GameData.Instance.LastPlayerPosition = p.position;
            GameData.Instance.LastPlayerRotation = p.rotation;
            GameData.Instance.WasFirstPerson = sv.IsInFirstPerson();
        }
    }
}