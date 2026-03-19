// 文件：GameData.cs
// 模块：核心管理器 / 游戏全局数据
// 说明：该类作为游戏全局数据的单例管理器，存储所有跨场景持久化的游戏状态、设置和资源引用。
//      新增数据持久化功能，通过 SaveGame() 和 LoadGame() 将数据保存到 persistentDataPath 的 save.json 文件中。
// 特性：单例模式，DontDestroyOnLoad，JSON序列化。

using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

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

    // 引导记录
    public List<string> CompletedGuideIds = new List<string>();
    // 答题印章收集记录
    public List<string> collectedStamps = new List<string>();
    // 任务是否已开始
    public bool questStarted = false;
    // 是否已领取导览员奖励
    public bool rewardClaimed = false;

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

    // =========================================================
    // 数据持久化相关
    // =========================================================
    [System.Serializable]
    private class SaveData
    {
        public bool HasPlayedIntro;
        public float BgmVolume;
        public float VideoVolume;
        public float VoiceVolume;
        public float ButtonVolume;
        public List<string> collectedStamps;
        public bool questStarted;
        public bool rewardClaimed;
        public List<string> CompletedGuideIds;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 保存游戏数据到文件
    /// </summary>
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.HasPlayedIntro = HasPlayedIntro;
        data.BgmVolume = BgmVolume;
        data.VideoVolume = VideoVolume;
        data.VoiceVolume = VoiceVolume;
        data.ButtonVolume = ButtonVolume;
        data.collectedStamps = collectedStamps;
        data.questStarted = questStarted;
        data.rewardClaimed = rewardClaimed;
        data.CompletedGuideIds = CompletedGuideIds;

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.persistentDataPath, "save.json");
        File.WriteAllText(path, json);
        Debug.Log("游戏已保存到: " + path);
    }

    /// <summary>
    /// 从文件加载游戏数据
    /// </summary>
    private void LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, "save.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                HasPlayedIntro = data.HasPlayedIntro;
                BgmVolume = data.BgmVolume;
                VideoVolume = data.VideoVolume;
                VoiceVolume = data.VoiceVolume;
                ButtonVolume = data.ButtonVolume;
                collectedStamps = data.collectedStamps ?? new List<string>();
                questStarted = data.questStarted;
                rewardClaimed = data.rewardClaimed;
                CompletedGuideIds = data.CompletedGuideIds ?? new List<string>();

                Debug.Log("游戏已加载");
            }
        }
        else
        {
            Debug.Log("无存档文件，使用默认设置");
        }
    }

    /// <summary>
    /// 重置游戏进度（清空印章、任务状态、奖励等）
    /// </summary>
    public void ResetProgress()
    {
        collectedStamps.Clear();
        questStarted = false;
        rewardClaimed = false;
        CompletedGuideIds.Clear();
        SaveGame();
        Debug.Log("游戏进度已重置");
    }

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

#if UNITY_EDITOR
    [ContextMenu("Reset Progress")]
    private void ResetProgressEditor()
    {
        ResetProgress();
    }
#endif
}