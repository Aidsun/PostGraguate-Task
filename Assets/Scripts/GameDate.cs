using UnityEngine;
using UnityEngine.Video;

public class GameDate : MonoBehaviour
{
    // 图片数据包
    public class ImageDate
    {
        public string Title;
        public string DescriptionText;
        public Sprite ImageShow;
        public AudioClip DescriptionAudio;
    }

    // 全局图片数据单例
    public static ImageDate CurrentImageData;
    //视频数据包
    public class VideoDate
    {
        public string Title;
        public VideoClip VideoFile;
        public string DescriptionText;
    }
    //全局视频数据单例
    public static VideoDate CurrentVideoDate;

    // 归档数据——位置和视角
    public static Vector3 LastPlayerPosition;
    public static Quaternion LastPlayerRotation;
    // 开关：告诉漫游场景是否需要恢复位置
    public static bool ShouldRestorePosition = false;
    // 存视角状态：true=第一人称，false=第三人称
    public static bool WasFirstPerson = true;
}