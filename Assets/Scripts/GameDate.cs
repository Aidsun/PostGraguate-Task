using UnityEngine;
using UnityEngine.Video;

public class GameDate : MonoBehaviour
{
    // ==========================================
    // 1. 图片展品数据包
    // ==========================================
    public class ImageDate
    {
        [Tooltip("标题")]
        public string Title;
        [Tooltip("图片文件")]
        public Sprite ImageFile;
        [Tooltip("文字描述")]
        public string DescriptionText;
        [Tooltip("解说音频")]
        public AudioClip DescriptionAudio;
    }
    // 当前选中的图片数据
    public static ImageDate CurrentImageData;

    // ==========================================
    // 2. 视频展品数据包
    // ==========================================
    public class VideoDate
    {
        [Tooltip("标题")]
        public string Title;
        [Tooltip("视频文件")]
        public VideoClip VideoFile;
        [Tooltip("文字描述")]
        public string DescriptionText;
        [Tooltip("解说音频")]
        public AudioClip DescriptionAudio;
    }
    // 当前选中的视频数据
    public static VideoDate CurrentVideoDate;

    // ==========================================
    // 3. 全景视频数据包
    // ==========================================
    public class PanoramaDate
    {
        [Tooltip("标题")]
        public string Title;
        [Tooltip("全景视频文件")]
        // 【修改】统一命名规范，改为大写开头
        public VideoClip PanoramaFile;
        [Tooltip("解说音频")]
        public AudioClip DescriptionAudio;
    }
    // 当前选中的全景数据
    public static PanoramaDate CurrentPanoramaDate;

    // ==========================================
    // 4. 高亮颜色配置
    // ==========================================
    public class HighColor
    {
        public Color unActiveColor = Color.blue;
        public Color isActiveColor = Color.white;
    }
    public static HighColor CurrentHighColor;

    // ==========================================
    // 5. 玩家位置归档 (用于返回展厅时恢复)
    // ==========================================
    public static Vector3 LastPlayerPosition;
    public static Quaternion LastPlayerRotation;

    // 是否需要恢复位置 (true = 需要恢复, false = 正常出生)
    public static bool ShouldRestorePosition = false;

    // 进入展品前的视角状态 (true = 第一人称, false = 第三人称)
    public static bool WasFirstPerson = true;
}