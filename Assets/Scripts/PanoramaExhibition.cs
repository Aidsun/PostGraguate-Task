using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class PanoramaExhibition : MonoBehaviour
{
    [Header("展品数据")]
    public string Title;
    public VideoClip PanoramaContent; // 全景视频文件
    public Sprite CoverImage;         // 预览封面
    // 全景通常不需要在观看时显示长文本，所以这里只存不传，或者仅用于编辑器预览
    [TextArea] public string DescriptionNote;

    [Header("解说设置")]
    public bool EnableVoice = true;
    public AudioClip VoiceClip;

    [Header("组件绑定")]
    public Renderer CoverRenderer;
    public Renderer OutlineRenderer;
    public TMP_Text TitleLabel;

    [Header("目标场景")]
    public string TargetScene = "PanoramaContent";

    void Start()
    {
        if (TitleLabel) TitleLabel.text = Title;

        if (CoverRenderer && CoverImage)
        {
            CoverRenderer.material.mainTexture = CoverImage.texture;
        }
    }

    public void SetHighlight(bool active)
    {
        if (OutlineRenderer && GameData.Instance)
        {
            OutlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    public void StartDisplay()
    {
        // 1. 保存状态
        SavePlayerState();

        // 2. 打包数据
        GameData.PanoramaPacket packet = new GameData.PanoramaPacket();
        packet.Title = this.Title;
        packet.PanoramaContent = this.PanoramaContent;
        // 全景包里没有Description字段，故不传
        packet.AutoPlayVoice = this.EnableVoice;
        packet.VoiceClip = this.VoiceClip;

        GameData.CurrentPanorama = packet;

        // 3. 跳转
        SceneLoading.LoadLevel(TargetScene);
    }

    private void SavePlayerState()
    {
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