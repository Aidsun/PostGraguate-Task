using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("=== 玩家状态存档 ===")]
    public Vector3 LastPlayerPosition;
    public Quaternion LastPlayerRotation;
    public bool WasFirstPerson = true;
    public bool ShouldRestorePosition = false;

    [Header("=== 全局配置参数 (带默认值) ===")]
    [Range(0, 1)] public float BgmVolume = 1.0f;
    [Range(0, 1)] public float VideoVolume = 1.0f;
    [Range(0, 1)] public float VoiceVolume = 1.0f;
    [Range(0, 1)] public float ButtonVolume = 1.0f;

    // 【新增】核心游戏参数 (直接在这里给默认值，解决所有0值问题)
    public float MoveSpeed = 5.0f;          // 默认移动速度
    public float JumpHeight = 1.2f;         // 默认跳跃高度
    public float InteractionDistance = 10.0f; // 默认交互距离
    public float StepDistance = 1.8f;       // 默认步长

    public Color HighlightColor = Color.yellow;
    [HideInInspector] public KeyCode VideoPauseKey = KeyCode.Space;

    [Header("=== 资源库 ===")]
    public VideoClip StartIntroVideo;
    public VideoClip StartLoopVideo;
    [HideInInspector] public bool HasPlayedIntro = false;

    public AudioClip HighlightSound;

    public List<Sprite> LoadingBackgrounds;
    public List<Sprite> ContentBackgrounds;

    // --- 数据包定义 ---
    [System.Serializable]
    public class ImagePacket
    {
        public string Title; public Sprite ImageContent; public string Description; public AudioClip VoiceClip; public bool AutoPlayVoice;
    }
    public static ImagePacket CurrentImage;

    [System.Serializable]
    public class VideoPacket
    {
        public string Title; public VideoClip VideoContent; public string Description; public AudioClip VoiceClip; public bool AutoPlayVoice;
    }
    public static VideoPacket CurrentVideo;

    [System.Serializable]
    public class PanoramaPacket
    {
        public string Title; public VideoClip PanoramaContent; public AudioClip VoiceClip; public bool AutoPlayVoice;
    }
    public static PanoramaPacket CurrentPanorama;

    // --- 辅助方法 ---
    public Sprite GetRandomLoadingBG()
    {
        if (LoadingBackgrounds == null || LoadingBackgrounds.Count == 0) return null;
        return LoadingBackgrounds[Random.Range(0, LoadingBackgrounds.Count)];
    }

    public Sprite GetRandomContentBG()
    {
        if (ContentBackgrounds == null || ContentBackgrounds.Count == 0) return null;
        return ContentBackgrounds[Random.Range(0, ContentBackgrounds.Count)];
    }
}