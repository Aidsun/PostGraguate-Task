// 文件：AudioManager.cs
// 模块：核心管理器 / 音频管理
// 说明：负责游戏全局音频的管理，包括背景音乐、视频音频、解说音频和按钮音效。
//      根据当前场景控制音频的播放、暂停和停止，并将音量设置应用到AudioMixer。
// 特性：单例模式，DontDestroyOnLoad跨场景持久化，使用AudioMixer统一控制音量，
//      订阅SceneManager.sceneLoaded事件监听场景切换。

using UnityEngine;
using UnityEngine.Audio;      // 音频混合器相关
using UnityEngine.SceneManagement; // 场景管理

public class AudioManager : MonoBehaviour
{
    // 单例实例，全局唯一访问点
    public static AudioManager Instance;

    [Header("=== 核心组件 ===")]
    // 主音频混合器，用于统一控制不同音频组的音量
    public AudioMixer mainMixer;

    [Header("=== 场景音频节点 (自动获取) ===")]
    // 背景音乐音频源，通常播放主旋律
    public AudioSource BgmSource;
    // 视频音频源，用于播放视频中的声音（如纪录片）
    public AudioSource VidSource;
    // 解说音频源，用于播放展品的语音解说
    public AudioSource DesSource;
    // 按钮音效音频源，用于播放UI交互音效
    public AudioSource BtnSource;

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
            return;
        }

        // 自动查找子物体中的AudioSource组件，并赋值给对应的变量
        // 使用?.操作符防止空引用异常
        BgmSource = transform.Find("BgmAudio")?.GetComponent<AudioSource>();
        VidSource = transform.Find("VidAudio")?.GetComponent<AudioSource>();
        DesSource = transform.Find("DesAudio")?.GetComponent<AudioSource>();
        BtnSource = transform.Find("BtnAudio")?.GetComponent<AudioSource>();
    }

    // 当脚本启用时，订阅场景加载事件
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 当脚本禁用时，取消订阅场景加载事件
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // =========================================================
    // 【核心修复】场景音频状态机 (严厉版)
    // =========================================================
    /// <summary>
    /// 场景加载完成后调用，根据场景名称控制音频播放状态。
    /// </summary>
    /// <param name="scene">加载的场景</param>
    /// <param name="mode">加载模式</param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果BGM源不存在或GameData实例为空，则无法执行后续逻辑，直接返回
        if (BgmSource == null || GameData.Instance == null) return;

        string sceneName = scene.name;
        // 从GameData中获取当前主题曲
        AudioClip themeSong = GameData.Instance.MainThemeSong;

        // 1. 进入主馆：播放/继续主题曲
        if (sceneName == "Museum_Main")
        {
            // 切回主馆，停止视频声音（如果有）
            if (VidSource) VidSource.Stop();

            if (themeSong != null)
            {
                // 如果当前BGM不是主题曲，则替换并从头播放
                if (BgmSource.clip != themeSong)
                {
                    BgmSource.clip = themeSong;
                    BgmSource.loop = true;       // 设置为循环播放
                    BgmSource.time = 0;           // 从开头开始
                    BgmSource.Play();
                }
                // 如果已经是主题曲但处于暂停状态，则恢复播放
                else if (!BgmSource.isPlaying)
                {
                    BgmSource.UnPause();
                }
            }
        }
        // 2. 进入开始界面：彻底停止
        else if (sceneName == "StartGame")
        {
            BgmSource.Stop();
            BgmSource.clip = null;   // 清除剪辑，释放资源
            if (VidSource) VidSource.Stop();
        }
        // 3. 其他所有场景 (视频、全景、Loading)：BGM 必须暂停
        else
        {
            // 不管现在在播什么，只要不是主馆，BGM 必须暂停
            if (BgmSource.isPlaying)
            {
                BgmSource.Pause();
            }

            // 注意：这里不要 Stop VidSource，因为视频场景马上要用它
        }

        // 每次场景加载后更新音量（确保音量设置与当前GameData一致）
        UpdateMixerVolume();
    }

    private void Start()
    {
        if (mainMixer == null) Debug.LogError("❌ Main Mixer 未赋值！");
        UpdateMixerVolume();
    }

    /// <summary>
    /// 从GameData读取当前音量值，并应用到AudioMixer的对应参数上。
    /// </summary>
    public void UpdateMixerVolume()
    {
        if (GameData.Instance == null || mainMixer == null) return;
        // 分别设置四个音频组的音量
        SetMixerVol("BGM_Vol", GameData.Instance.BgmVolume);
        SetMixerVol("Video_Vol", GameData.Instance.VideoVolume);
        SetMixerVol("Voice_Vol", GameData.Instance.VoiceVolume);
        SetMixerVol("SFX_Vol", GameData.Instance.ButtonVolume);
    }

    /// <summary>
    /// 将线性音量值转换为对数分贝值，并设置到AudioMixer的参数。
    /// AudioMixer的音量参数期望的是分贝（dB）值，范围通常为 -80 到 20。
    /// </summary>
    /// <param name="paramName">AudioMixer中的参数名</param>
    /// <param name="linearVol">线性音量，范围0-1</param>
    private void SetMixerVol(string paramName, float linearVol)
    {
        // 将线性音量转换为分贝：dB = 20 * log10(linear)
        // 使用Max防止值为0导致log10负无穷
        float dbVol = Mathf.Log10(Mathf.Max(0.0001f, linearVol)) * 20;
        mainMixer.SetFloat(paramName, dbVol);
    }

    /// <summary>
    /// 播放高亮音效（通常当玩家聚焦于某个可交互物体时播放）
    /// </summary>
    public void PlayHighlightSound()
    {
        // 检查GameData是否存在、高亮音效是否存在、按钮音频源是否存在
        if (GameData.Instance && GameData.Instance.HighlightSound && BtnSource)
            BtnSource.PlayOneShot(GameData.Instance.HighlightSound);
    }

    /// <summary>
    /// 播放点击音效（通常当玩家点击按钮或交互时播放）
    /// </summary>
    public void PlayClickSound()
    {
        if (GameData.Instance && GameData.Instance.ButtonClickSound && BtnSource)
            BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
    }
}