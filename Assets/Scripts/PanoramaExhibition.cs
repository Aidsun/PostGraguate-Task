// 文件：PanoramaExhibition.cs
// 模块：交互对象 / 全景展品
// 说明：该脚本挂载在主馆场景中的全景展品对象上，负责全景展品的数据配置、外观初始化（显示封面图）、
//      高亮反馈以及交互启动。当玩家与展品交互时，它会保存当前玩家状态到GameData的保险箱，
//      将展品数据打包存入GameData.CurrentPanorama，然后跳转到全景展示场景（PanoramaContent）。
// 特性：使用[Header]、[TextArea]优化Inspector，通过SendMessage被PlayerInteraction调用，
//      依赖SwitchViews获取玩家视角状态，依赖SceneLoading进行场景切换。

using UnityEngine;
using UnityEngine.Video;      // 使用VideoClip类型
using TMPro;                  // 使用TextMeshPro文本组件

public class PanoramaExhibition : MonoBehaviour
{
    [Header("展品数据")]        // 在Inspector中分组显示
    public string Title;                     // 展品标题
    public VideoClip PanoramaContent;        // 全景视频文件（VideoClip类型）
    public Sprite CoverImage;                 // 预览封面图片（用于在3D物体上显示）

    // 全景通常不需要在观看时显示长文本，所以这里只存不传，或者仅用于编辑器预览
    [TextArea] public string DescriptionNote; // 编辑器专用描述文本（不传递到展示场景）

    [Header("解说设置")]
    public bool EnableVoice = true;           // 是否启用解说语音
    public AudioClip VoiceClip;                // 解说音频剪辑

    [Header("组件绑定")]
    public Renderer CoverRenderer;             // 用于显示封面图的3D物体的Renderer组件
    public Renderer OutlineRenderer;           // 用于显示高亮边框的物体的Renderer组件
    public TMP_Text TitleLabel;                // 显示展品标题的3D文本组件（可选）

    [Header("目标场景")]
    public string TargetScene = "PanoramaContent";   // 点击后要跳转的目标场景名称

    void Start()
    {
        // 初始化标题文本（如果存在）
        if (TitleLabel) TitleLabel.text = Title;

        // 如果有封面渲染器且封面图片存在，则将图片的纹理赋值给材质的主纹理
        // 这样展品表面就能显示封面图
        if (CoverRenderer && CoverImage)
        {
            CoverRenderer.material.mainTexture = CoverImage.texture;
        }
    }

    /// <summary>
    /// 设置高亮状态，由PlayerInteraction通过SendMessage调用。
    /// 当玩家射线击中该物体时，会调用此方法并传入true；离开时传入false。
    /// </summary>
    /// <param name="active">是否激活高亮</param>
    public void SetHighlight(bool active)
    {
        // 如果有边框渲染器且GameData实例存在，则根据active改变颜色
        if (OutlineRenderer && GameData.Instance)
        {
            OutlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    /// <summary>
    /// 开始展示，由PlayerInteraction在玩家点击时通过SendMessage调用。
    /// 执行保存状态、打包数据、全局赋值、场景跳转等一系列操作。
    /// </summary>
    public void StartDisplay()
    {
        // 1. 保存当前玩家状态（位置、视角等）到保险箱
        SavePlayerState();

        // 2. 打包当前展品的数据到PanoramaPacket对象
        GameData.PanoramaPacket packet = new GameData.PanoramaPacket();
        packet.Title = this.Title;
        packet.PanoramaContent = this.PanoramaContent;   // 直接传递VideoClip
        packet.AutoPlayVoice = this.EnableVoice;
        packet.VoiceClip = this.VoiceClip;               // 直接传递AudioClip

        // 3. 将数据包存入GameData的静态字段，供下一个场景（PanoramaContent）使用
        GameData.CurrentPanorama = packet;

        // 4. 通过SceneLoading的静态方法跳转到全景展示场景
        SceneLoading.LoadLevel(TargetScene);
    }

    /// <summary>
    /// 保存玩家当前状态到GameData的保险箱（TempSafeState），用于从展示场景返回时恢复。
    /// </summary>
    private void SavePlayerState()
    {
        // 查找场景中的SwitchViews脚本（负责视角切换和玩家位置管理）
        SwitchViews sv = FindObjectOfType<SwitchViews>();

        if (sv && GameData.Instance)
        {
            // 获取当前激活的玩家角色的Transform（第一人称或第三人称）
            Transform p = sv.GetActivePlayerTransform();
            if (p)
            {
                // =========================================================
                // 【核心修改】直接存入保险箱 (TempSafeState)
                // =========================================================
                // 创建PlayerStateData结构，填充当前状态
                GameData.PlayerStateData safeData = new GameData.PlayerStateData();
                safeData.Position = p.position;
                safeData.Rotation = p.rotation;
                safeData.IsFirstPerson = sv.IsInFirstPerson();   // 记录当前视角模式
                safeData.HasData = true;   // 标记有有效数据

                // 存入GameData的保险箱
                GameData.Instance.TempSafeState = safeData;

                // 【重要】确保不要触发普通的恢复逻辑 (防止在全景场景掉入虚空)
                // 禁用自动恢复标记，防止进入全景场景时触发意外的位置恢复
                GameData.Instance.ShouldRestorePosition = false;

                Debug.Log($"[Panorama] 状态已安全封存。视角模式: {(safeData.IsFirstPerson ? "第一人称" : "第三人称")}");
            }
        }
    }
}