using UnityEngine;

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

    // 全局数据
    public static ImageDate CurrentImageData;

    // 存位置
    public static Vector3 LastPlayerPosition;
    public static Quaternion LastPlayerRotation;

    // 开关：告诉漫游场景是否需要恢复位置
    public static bool ShouldRestorePosition = false;

    // 【关键修改】取消注释：存视角状态（true=第一人称，false=第三人称）
    public static bool WasFirstPerson = true;
}