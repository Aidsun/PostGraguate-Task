// 文件：GameData.cs
// 模块：核心管理器 / 游戏全局数据
// 说明：该类作为游戏全局数据的单例管理器，存储所有跨场景持久化的游戏状态、设置和资源引用。
//      包括玩家状态（保险箱数据）、音量控制、全局音频资源、游戏参数、交互设置、引导记录、位置记忆、
//      背景图库以及当前展示场景的数据包（ImagePacket/VideoPacket/PanoramaPacket）。
// 特性：单例模式，DontDestroyOnLoad跨场景持久化，使用[System.Serializable]定义可序列化结构，
//      使用[Header]、[Range]、[Space]、[HideInInspector]等特性优化Inspector显示。

using UnityEngine;
using UnityEngine.Video;      // 使用VideoClip类型
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    // 单例实例，全局唯一访问点
    public static GameData Instance;

    private void Awake()
    {
        // 标准单例实现：如果不存在则创建并保持，否则销毁新对象
        if (Instance == null)
        {
            Instance = this;
            // 使该游戏对象在加载新场景时不被销毁
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 已存在实例，销毁当前对象
            Destroy(gameObject);
        }
    }

    // =========================================================
    // 【核心结构】保险箱数据
    // =========================================================
    /// <summary>
    /// 玩家状态数据结构，用于在场景切换前临时保存玩家位置、视角模式等信息。
    /// 相当于一个“保险箱”，当从主馆跳转到展示场景时，将玩家状态存入此处，
    /// 返回主馆时再恢复。
    /// </summary>
    [System.Serializable]   // 使该结构可以在Inspector中显示，并支持序列化
    public struct PlayerStateData
    {
        public Vector3 Position;      // 玩家位置
        public Quaternion Rotation;    // 玩家旋转
        public bool IsFirstPerson;     // 是否为第一人称视角
        public bool HasData;           // 标记是否存有有效数据
    }

    // 临时存储的玩家状态数据
    public PlayerStateData TempSafeState;

    // =========================================================

    [Header("=== 1. 全局状态记录 ===")]
    // 是否已经播放过开场动画，用于控制开场动画只播放一次
    public bool HasPlayedIntro = false;

    [Header("=== 2. 全局音量控制 ===")]
    // Range特性限制音量值在0~1之间，并在Inspector中显示为滑块
    [Range(0, 1)] public float BgmVolume = 1.0f;     // 背景音乐音量
    [Range(0, 1)] public float VideoVolume = 1.0f;   // 视频音量
    [Range(0, 1)] public float VoiceVolume = 1.0f;   // 解说语音音量
    [Range(0, 1)] public float ButtonVolume = 1.0f;  // 按钮音效音量

    [Header("=== 3. 全局音频资源 ===")]
    // 直接在Inspector中拖拽的音频剪辑，供AudioManager等脚本使用
    public AudioClip ButtonClickSound;   // 按钮点击音效
    public AudioClip HighlightSound;     // 高亮提示音效（当玩家聚焦可交互物体时）
    public AudioClip PanelOpenSound;     // 面板打开音效
    public AudioClip MainThemeSong;      // 主馆背景音乐

    [Header("=== 4. 游戏参数 ===")]
    public Color HighlightColor = Color.yellow;   // 物体高亮时的颜色
    public float MoveSpeed = 5.0f;                 // 玩家移动速度
    public float JumpHeight = 1.2f;                // 跳跃高度
    public float InteractionDistance = 20.0f;      // 玩家可交互的最大距离
    public float StepDistance = 1.8f;               // 脚步声步长（移动多少米触发一次脚步声）

    // HideInInspector使该字段在Inspector中隐藏，但仍可在代码中访问和序列化
    [HideInInspector] public KeyCode VideoPauseKey = KeyCode.Space;   // 视频播放时暂停/继续的按键

    [Space(10)]   // 在Inspector中添加10像素的垂直间距
    [Header("=== 5. 交互设置 ===")]
    public bool AllowSkipIntro = true;   // 是否允许跳过开场动画

    // 【新增】永久记录已触发的引导ID (防复活名单)
    // 用于存储已经触发过的引导提示的ID，防止同一引导反复出现
    public List<string> CompletedGuideIds = new List<string>();

    // 玩家位置记忆
    public bool ShouldRestorePosition = false;      // 标记是否需要在进入主馆时恢复玩家位置
    public Vector3 LastPlayerPosition;               // 最后记录的玩家位置
    public Quaternion LastPlayerRotation;            // 最后记录的玩家旋转
    public bool WasFirstPerson = true;               // 最后记录的视角模式

    // 资源库
    public List<Sprite> ContentBackgrounds;   // 展示场景中内容区域的随机背景图库
    public List<Sprite> LoadingBackgrounds;   // 加载场景中的随机背景图库

    // --- 数据包定义 ---
    // 这些类用于在不同场景间传递展品数据，例如从主馆的展品对象传递到展示场景
    [System.Serializable]
    public class ImagePacket
    {
        public string Title;           // 展品标题
        public Sprite ImageContent;    // 图片内容
        public string Description;      // 描述文字
        public AudioClip VoiceClip;     // 解说音频
        public bool AutoPlayVoice;      // 是否自动播放解说
    }
    public static ImagePacket CurrentImage;   // 当前正在处理的图文展品数据

    [System.Serializable]
    public class VideoPacket
    {
        public string Title;
        public VideoClip VideoContent;  // 视频内容
        public string Description;
        public AudioClip VoiceClip;
        public bool AutoPlayVoice;
    }
    public static VideoPacket CurrentVideo;   // 当前正在处理的视频展品数据

    [System.Serializable]
    public class PanoramaPacket
    {
        public string Title;
        public VideoClip PanoramaContent;  // 全景视频内容
        public AudioClip VoiceClip;
        public bool AutoPlayVoice;
    }
    public static PanoramaPacket CurrentPanorama;   // 当前正在处理的全景展品数据

    // 辅助方法
    /// <summary>
    /// 从内容背景图库中随机返回一张Sprite。
    /// </summary>
    public Sprite GetRandomContentBG()
    {
        if (ContentBackgrounds == null || ContentBackgrounds.Count == 0) return null;
        return ContentBackgrounds[Random.Range(0, ContentBackgrounds.Count)];
    }

    /// <summary>
    /// 从加载背景图库中随机返回一张Sprite。
    /// </summary>
    public Sprite GetRandomLoadingBG()
    {
        if (LoadingBackgrounds == null || LoadingBackgrounds.Count == 0) return null;
        return LoadingBackgrounds[Random.Range(0, LoadingBackgrounds.Count)];
    }
}