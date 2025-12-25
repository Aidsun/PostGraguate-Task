using UnityEngine;
using UnityEngine.Video;

public class GameDate : MonoBehaviour
{
    [Tooltip("图片数据类型包")]
    public class ImageDate
    {
        [Tooltip("图片标题")]
        public string Title;
        [Tooltip("图片文件")]
        public Sprite ImageFile;
        [Tooltip("图片描述文本")]
        public string DescriptionText;
        [Tooltip("图片描述配音")]
        public AudioClip DescriptionAudio;
    }
    [Tooltip("当前图片数据包")]
    public static ImageDate CurrentImageData;

    [Tooltip("视频数据类型包")]
    public class VideoDate
    {
        [Tooltip("视频标题")]
        public string Title;
        [Tooltip("视频文件")]
        public VideoClip VideoFile;
        [Tooltip("视频描述文本")]
        public string DescriptionText;
        [Tooltip("视频描述配音")]
        public AudioClip DescriptionAudio;
    }
    [Tooltip("当前视频数据包")]
    public static VideoDate CurrentVideoDate;

    [Tooltip("全景视频数据类型包")]
    public class PanoramaDate
    {
        [Tooltip("全景视频标题")]
        public string Title;
        [Tooltip("全景视频文件")]
        public VideoClip panoramaFile;
        [Tooltip("全景视频描述配音")]
        public AudioClip DescriptionAudio;
    }
    [Tooltip("当前全景视频数据包")]
    public static PanoramaDate CurrentPanoramaDate;

    // 高亮颜色数据包
    public class HighColor
    {
        public Color unActiveColor = Color.blue;
        public Color isActiveColor = Color.white;
    }
    public static HighColor CurrentHighColor;

    // 归档数据
    public static Vector3 LastPlayerPosition;
    public static Quaternion LastPlayerRotation;
    public static bool ShouldRestorePosition = false;
    public static bool WasFirstPerson = true;
}