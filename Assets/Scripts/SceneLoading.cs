using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio; // 引入命名空间
using System.Collections;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class SceneLoading : MonoBehaviour
{
    [Header("=== UI 组件 ===")]
    public Slider progressBar;
    public TMP_Text progressText;
    public Image backgroundRenderer;

    [Header("=== 加载设置 ===")]
    [Range(1, 10)] public float minLoadTime = 3.0f;
    public AudioClip loadingClip;

    [Header("=== 音频输出设置 (必填) ===")]
    // 【新增】允许你在编辑器里把 Mixer 的 BGM 组拖进来
    public AudioMixerGroup outputGroup;

    public static string SceneToLoad;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // 【关键修复】如果配置了 Mixer 组，就应用上去
        if (outputGroup != null)
        {
            audioSource.outputAudioMixerGroup = outputGroup;
        }
    }

    private void Start()
    {
        // 1. 初始化背景
        if (GameData.Instance != null && backgroundRenderer != null)
        {
            Sprite randomBG = GameData.Instance.GetRandomLoadingBG();
            if (randomBG != null) backgroundRenderer.sprite = randomBG;
        }

        // 2. 播放音乐 (音量完全由 Mixer 控制，代码里不需设 volume)
        if (loadingClip != null && audioSource != null)
        {
            audioSource.clip = loadingClip;
            audioSource.Play();
        }

        // 3. 异步加载
        if (!string.IsNullOrEmpty(SceneToLoad))
        {
            StartCoroutine(LoadAsync(SceneToLoad));
        }
    }

    // ... (LoadAsync 和 LoadLevel 方法保持不变，无需修改) ...
    private IEnumerator LoadAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        float timer = 0f;
        while (operation.progress < 0.9f || timer < minLoadTime)
        {
            timer += Time.deltaTime;
            float displayProgress = Mathf.Min(Mathf.Clamp01(operation.progress / 0.9f), Mathf.Clamp01(timer / minLoadTime));
            if (progressBar) progressBar.value = displayProgress;
            if (progressText) progressText.text = $"资源加载中... {(displayProgress * 100):F0}%";
            yield return null;
        }
        if (progressBar) progressBar.value = 1f;
        if (progressText) progressText.text = "加载完成! 100%";
        yield return new WaitForSeconds(0.5f);
        operation.allowSceneActivation = true;
    }

    public static void LoadLevel(string sceneName)
    {
        SceneToLoad = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }
}