using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== 核心组件 ===")]
    public AudioMixer mainMixer; // 记得在 Inspector 确认拖入 MainMixer

    [Header("=== 场景音频节点 (自动获取) ===")]
    public AudioSource BgmSource;
    public AudioSource VidSource;
    public AudioSource DesSource;
    public AudioSource BtnSource;

    private void Awake()
    {
        Instance = this;
        // 自动查找
        BgmSource = transform.Find("BgmAudio")?.GetComponent<AudioSource>();
        VidSource = transform.Find("VidAudio")?.GetComponent<AudioSource>();
        DesSource = transform.Find("DesAudio")?.GetComponent<AudioSource>();
        BtnSource = transform.Find("BtnAudio")?.GetComponent<AudioSource>();
    }

    private void Start()
    {
        // --- 调试日志：检查 Mixer 是否连接 ---
        if (mainMixer == null)
            Debug.LogError("❌ [AudioManager] 严重错误：Main Mixer 未赋值！请在 All_Audios 面板里拖入文件！");
        else
            Debug.Log("✅ [AudioManager] Mixer 连接成功。");

        UpdateMixerVolume();
    }

    private void Update()
    {
        UpdateMixerVolume();
    }

    private void UpdateMixerVolume()
    {
        if (GameData.Instance == null || mainMixer == null) return;

        // 实时设置混音器参数
        SetMixerVol("BGM_Vol", GameData.Instance.BgmVolume);
        SetMixerVol("Video_Vol", GameData.Instance.VideoVolume);
        SetMixerVol("Voice_Vol", GameData.Instance.VoiceVolume);
        SetMixerVol("SFX_Vol", GameData.Instance.ButtonVolume);
    }

    private void SetMixerVol(string paramName, float linearVol)
    {
        // 0-1 转 分贝
        float dbVol = Mathf.Log10(Mathf.Max(0.0001f, linearVol)) * 20;
        bool result = mainMixer.SetFloat(paramName, dbVol);

        // 如果名字写错了，这里会报黄字警告
        if (!result) Debug.LogWarning($"⚠️ 无法找到Mixer参数: {paramName}，请检查AudioMixer面板里的名字是否完全一致！");
    }

    // === 之前漏掉的方法，现在补上 ===
    public void PlayHighlightSound()
    {
        if (GameData.Instance && GameData.Instance.HighlightSound && BtnSource)
            BtnSource.PlayOneShot(GameData.Instance.HighlightSound);
    }

    public void PlayClickSound()
    {
        if (GameData.Instance && GameData.Instance.ButtonClickSound && BtnSource)
            BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
    }
}