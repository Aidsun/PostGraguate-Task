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

    [Header("=== 1. 全局状态记录 ===")]
    public bool HasPlayedIntro = false;

    [Header("=== 2. 全局音量控制 (数据源, 0-1) ===")]
    [Range(0, 1)] public float BgmVolume = 1.0f;
    [Range(0, 1)] public float VideoVolume = 1.0f;
    [Range(0, 1)] public float VoiceVolume = 1.0f;
    [Range(0, 1)] public float ButtonVolume = 1.0f;

    [Header("=== 3. 全局通用音频资源 ===")]
    public AudioClip ButtonClickSound; // 通用的点击“咔哒”声
    public AudioClip HighlightSound;   // 通用的高亮提示音
    // 【新增】面板打开音效
    public AudioClip PanelOpenSound;

    [Header("=== 4. 游戏核心参数 ===")]
    public Color HighlightColor = Color.yellow;

    public float MoveSpeed = 5.0f;
    public float JumpHeight = 1.2f;
    public float InteractionDistance = 10.0f;
    public float StepDistance = 1.8f;
    [HideInInspector] public KeyCode VideoPauseKey = KeyCode.Space;

    // 玩家位置记忆
    public bool ShouldRestorePosition = false;
    public Vector3 LastPlayerPosition;
    public Quaternion LastPlayerRotation;
    public bool WasFirstPerson = true;

    // 资源库
    public List<Sprite> ContentBackgrounds;
    public List<Sprite> LoadingBackgrounds;

    // --- 跨场景传递的数据包 ---
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

    // 辅助方法
    public Sprite GetRandomContentBG()
    {
        if (ContentBackgrounds == null || ContentBackgrounds.Count == 0) return null;
        return ContentBackgrounds[Random.Range(0, ContentBackgrounds.Count)];
    }
    public Sprite GetRandomLoadingBG()
    {
        if (LoadingBackgrounds == null || LoadingBackgrounds.Count == 0) return null;
        return LoadingBackgrounds[Random.Range(0, LoadingBackgrounds.Count)];
    }
}