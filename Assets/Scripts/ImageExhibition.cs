using UnityEngine;
using TMPro;

public class ImageExhibition : MonoBehaviour
{
    [Header("数据配置")]
    public string Title;
    public Sprite ImageContent;
    [TextArea] public string Description;
    public bool EnableVoice = true;
    public AudioClip VoiceClip;

    [Header("组件")]
    public Renderer CoverRenderer;
    public Renderer OutlineRenderer;
    public TMP_Text TitleLabel;

    void Start()
    {
        if (TitleLabel) TitleLabel.text = Title;
        if (CoverRenderer && ImageContent)
        {
            CoverRenderer.material.mainTexture = ImageContent.texture;
        }
    }

    public void SetHighlight(bool active)
    {
        if (OutlineRenderer && GameData.Instance)
            OutlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
    }

    public void StartDisplay()
    {
        // 1. 保存当前状态 (位置/视角)
        SaveState();

        // 2. 打包数据
        GameData.ImagePacket packet = new GameData.ImagePacket();
        packet.Title = this.Title;
        packet.ImageContent = this.ImageContent;
        packet.Description = this.Description;
        packet.AutoPlayVoice = this.EnableVoice;
        packet.VoiceClip = this.VoiceClip;

        // 3. 存入全局
        GameData.CurrentImage = packet;

        // 4. 跳转
        SceneLoading.LoadLevel("ImageContent"); // 确保您的场景名是这个
    }

    private void SaveState()
    {
        // 找到 SwitchViews 脚本来获取玩家位置
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