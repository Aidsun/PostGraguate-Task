using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// 场景加载管理器：负责在场景之间平滑过渡，展示进度条，并处理数据传输。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneLoading : MonoBehaviour
{
    [Header("=== UI 组件 ===")]
    public Slider progressBar;          // 进度条
    public TMP_Text progressText;       // 进度百分比文本
    public Image backgroundRenderer;    // 用于显示随机背景图的组件

    [Header("=== 加载设置 ===")]
    [Tooltip("最小加载时间(秒)，防止加载太快看不到背景图")]
    [Range(1, 10)] public float minLoadTime = 3.0f;

    [Tooltip("加载时的背景音乐")]
    public AudioClip loadingClip;

    // 静态变量：下一个要去的场景名
    public static string SceneToLoad;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true; // 加载音乐通常循环
    }

    private void Start()
    {
        // 1. 初始化背景图 (从 GameData 获取随机图)
        if (GameData.Instance != null && backgroundRenderer != null)
        {
            Sprite randomBG = GameData.Instance.GetRandomLoadingBG();
            if (randomBG != null) backgroundRenderer.sprite = randomBG;
        }

        // 2. 播放加载音乐 (受 BGM 音量控制)
        if (loadingClip != null && audioSource != null)
        {
            float vol = (GameData.Instance != null) ? GameData.Instance.BgmVolume : 1.0f;
            audioSource.volume = vol;
            audioSource.clip = loadingClip;
            audioSource.Play();
        }

        // 3. 开始异步加载
        if (!string.IsNullOrEmpty(SceneToLoad))
        {
            StartCoroutine(LoadAsync(SceneToLoad));
        }
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        // 开始异步加载场景，但不允许立即切换
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        // 模拟加载过程 (结合真实进度和最小等待时间)
        while (operation.progress < 0.9f || timer < minLoadTime)
        {
            timer += Time.deltaTime;

            // 计算进度：真实进度(0.9 max) 和 时间进度 的最小值
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(timer / minLoadTime);
            float displayProgress = Mathf.Min(loadProgress, timeProgress);

            // 更新 UI
            if (progressBar) progressBar.value = displayProgress;
            if (progressText) progressText.text = $"资源加载中... {(displayProgress * 100):F0}%";

            yield return null;
        }

        // 加载完成
        if (progressBar) progressBar.value = 1f;
        if (progressText) progressText.text = "加载完成! 100%";

        yield return new WaitForSeconds(0.5f); // 稍微停顿一下展示 100%

        // 允许切换场景
        operation.allowSceneActivation = true;
    }

    /// <summary>
    /// 静态调用入口：任何脚本调用此方法即可跳转场景
    /// </summary>
    public static void LoadLevel(string sceneName)
    {
        SceneToLoad = sceneName;
        // 跳转到由 SceneLoading 脚本所在的场景（通常叫 LoadingScene）
        SceneManager.LoadScene("LoadingScene");
    }
}