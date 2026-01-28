using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== 核心组件 ===")]
    public AudioMixer mainMixer;

    [Header("=== 场景音频节点 (自动获取) ===")]
    public AudioSource BgmSource;
    public AudioSource VidSource;
    public AudioSource DesSource;
    public AudioSource BtnSource;

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
            return;
        }

        // 自动查找子物体
        BgmSource = transform.Find("BgmAudio")?.GetComponent<AudioSource>();
        VidSource = transform.Find("VidAudio")?.GetComponent<AudioSource>();
        DesSource = transform.Find("DesAudio")?.GetComponent<AudioSource>();
        BtnSource = transform.Find("BtnAudio")?.GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // =========================================================
    // 【核心修复】场景音频状态机 (严厉版)
    // =========================================================
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (BgmSource == null || GameData.Instance == null) return;

        string sceneName = scene.name;
        AudioClip themeSong = GameData.Instance.MainThemeSong;

        // 1. 进入主馆：播放/继续主题曲
        if (sceneName == "Museum_Main")
        {
            if (VidSource) VidSource.Stop(); // 切回主馆，把视频声音掐断

            if (themeSong != null)
            {
                if (BgmSource.clip != themeSong)
                {
                    BgmSource.clip = themeSong;
                    BgmSource.loop = true;
                    BgmSource.time = 0;
                    BgmSource.Play();
                }
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
            BgmSource.clip = null;
            if (VidSource) VidSource.Stop();
        }
        // 3. 其他所有场景 (视频、全景、Loading)：BGM 必须暂停
        else
        {
            // 不管现在在播什么，只要不是主馆，BGM 必须闭嘴
            if (BgmSource.isPlaying)
            {
                BgmSource.Pause();
            }

            // 注意：这里不要 Stop VidSource，因为视频场景马上要用它
        }

        UpdateMixerVolume();
    }

    private void Start()
    {
        if (mainMixer == null) Debug.LogError("❌ Main Mixer 未赋值！");
        UpdateMixerVolume();
    }

    public void UpdateMixerVolume()
    {
        if (GameData.Instance == null || mainMixer == null) return;
        SetMixerVol("BGM_Vol", GameData.Instance.BgmVolume);
        SetMixerVol("Video_Vol", GameData.Instance.VideoVolume);
        SetMixerVol("Voice_Vol", GameData.Instance.VoiceVolume);
        SetMixerVol("SFX_Vol", GameData.Instance.ButtonVolume);
    }

    private void SetMixerVol(string paramName, float linearVol)
    {
        float dbVol = Mathf.Log10(Mathf.Max(0.0001f, linearVol)) * 20;
        mainMixer.SetFloat(paramName, dbVol);
    }

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