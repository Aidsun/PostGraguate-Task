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

    // =========================================================
    // 【核心结构】保险箱数据
    // =========================================================
    [System.Serializable]
    public struct PlayerStateData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsFirstPerson;
        public bool HasData;
    }
    public PlayerStateData TempSafeState;

    // =========================================================

    [Header("=== 1. 全局状态记录 ===")]
    public bool HasPlayedIntro = false;

    [Header("=== 2. 全局音量控制 ===")]
    [Range(0, 1)] public float BgmVolume = 1.0f;
    [Range(0, 1)] public float VideoVolume = 1.0f;
    [Range(0, 1)] public float VoiceVolume = 1.0f;
    [Range(0, 1)] public float ButtonVolume = 1.0f;

    [Header("=== 3. 全局音频资源 ===")]
    public AudioClip ButtonClickSound;
    public AudioClip HighlightSound;
    public AudioClip PanelOpenSound;
    public AudioClip MainThemeSong;

    [Header("=== 4. 游戏参数 ===")]
    public Color HighlightColor = Color.yellow;
    public float MoveSpeed = 5.0f;
    public float JumpHeight = 1.2f;
    public float InteractionDistance = 20.0f;
    public float StepDistance = 1.8f;
    [HideInInspector] public KeyCode VideoPauseKey = KeyCode.Space;

    [Space(10)]
    [Header("=== 5. 交互设置 ===")]
    public bool AllowSkipIntro = true;

    // 【新增】永久记录已触发的引导ID (防复活名单)
    public List<string> CompletedGuideIds = new List<string>();

    // 玩家位置记忆
    public bool ShouldRestorePosition = false;
    public Vector3 LastPlayerPosition;
    public Quaternion LastPlayerRotation;
    public bool WasFirstPerson = true;

    // 资源库
    public List<Sprite> ContentBackgrounds;
    public List<Sprite> LoadingBackgrounds;

    // --- 数据包定义 ---
    [System.Serializable]
    public class ImagePacket { public string Title; public Sprite ImageContent; public string Description; public AudioClip VoiceClip; public bool AutoPlayVoice; }
    public static ImagePacket CurrentImage;

    [System.Serializable]
    public class VideoPacket { public string Title; public VideoClip VideoContent; public string Description; public AudioClip VoiceClip; public bool AutoPlayVoice; }
    public static VideoPacket CurrentVideo;

    [System.Serializable]
    public class PanoramaPacket { public string Title; public VideoClip PanoramaContent; public AudioClip VoiceClip; public bool AutoPlayVoice; }
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