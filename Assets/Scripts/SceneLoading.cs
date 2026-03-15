// 文件：SceneLoading.cs
// 模块：核心管理器 / 场景加载
// 说明：该脚本挂载在加载场景（LoadingScene）中的对象上，负责异步加载目标场景，
//      并在加载过程中显示进度条、进度文字和随机背景图。同时可播放加载音乐，
//      并将音乐输出到指定的AudioMixer组。
// 特性：RequireComponent强制依赖AudioSource，使用协程进行异步加载，通过静态字段传递目标场景名，
//      使用Slider和TMP_Text显示进度，从GameData获取随机背景图。

using UnityEngine;
using UnityEngine.SceneManagement;      // 场景管理
using UnityEngine.UI;                    // UI组件（Slider, Image）
using UnityEngine.Audio;                  // 音频混合器
using System.Collections;                 // 协程
using TMPro;                               // TextMeshPro文本组件

[RequireComponent(typeof(AudioSource))]    // 自动添加AudioSource组件，如果缺失则自动创建
public class SceneLoading : MonoBehaviour
{
    [Header("=== UI 组件 ===")]            // Inspector分组
    public Slider progressBar;              // 进度条滑块
    public TMP_Text progressText;            // 进度文字（显示百分比）
    public Image backgroundRenderer;         // 背景图片渲染器

    [Header("=== 加载设置 ===")]
    [Range(1, 10)] public float minLoadTime = 3.0f;   // 最小加载时间，确保加载界面显示至少3秒
    public AudioClip loadingClip;                       // 加载时播放的音乐剪辑

    [Header("=== 音频输出设置 (必填) ===")]
    // 【新增】允许你在编辑器里把 Mixer 的 BGM 组拖进来
    public AudioMixerGroup outputGroup;     // 指定音频输出到的Mixer组，用于音量控制

    public static string SceneToLoad;        // 静态字段，由其他脚本（如StartGame）设置，指定要加载的目标场景名称
    private AudioSource audioSource;         // 音频源组件引用

    private void Awake()
    {
        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 配置音频源：不在开始时自动播放，不循环
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // 【关键修复】如果配置了 Mixer 组，就应用上去
        if (outputGroup != null)
        {
            audioSource.outputAudioMixerGroup = outputGroup;
        }
    }

    private void Start()
    {
        // 1. 初始化背景
        // 从GameData中获取随机加载背景图，并赋值给背景图片
        if (GameData.Instance != null && backgroundRenderer != null)
        {
            Sprite randomBG = GameData.Instance.GetRandomLoadingBG();
            if (randomBG != null) backgroundRenderer.sprite = randomBG;
        }

        // 2. 播放音乐 (音量完全由 Mixer 控制，代码里不需设 volume)
        if (loadingClip != null && audioSource != null)
        {
            audioSource.clip = loadingClip;
            audioSource.Play();   // 播放加载音乐
        }

        // 3. 异步加载目标场景
        if (!string.IsNullOrEmpty(SceneToLoad))
        {
            StartCoroutine(LoadAsync(SceneToLoad));
        }
    }

    /// <summary>
    /// 异步加载场景的协程，显示加载进度并控制最小加载时间。
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    private IEnumerator LoadAsync(string sceneName)
    {
        // 开始异步加载场景，但不允许立即激活（allowSceneActivation = false）
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;
        // 循环直到加载进度达到0.9（即加载完成）且计时器超过最小加载时间
        while (operation.progress < 0.9f || timer < minLoadTime)
        {
            timer += Time.deltaTime;
            // 计算显示进度：取加载进度（映射到0-1）和计时器进度的最小值，确保不会提前显示100%
            float displayProgress = Mathf.Min(
                Mathf.Clamp01(operation.progress / 0.9f),   // 加载进度归一化（因为0.9对应100%）
                Mathf.Clamp01(timer / minLoadTime)          // 计时器进度
            );
            if (progressBar) progressBar.value = displayProgress;
            if (progressText) progressText.text = $"资源加载中... {(displayProgress * 100):F0}%";
            yield return null;   // 等待下一帧
        }

        // 达到条件后，将进度条设为1，文字设为100%
        if (progressBar) progressBar.value = 1f;
        if (progressText) progressText.text = "加载完成! 100%";
        yield return new WaitForSeconds(0.5f);   // 短暂停留，让玩家看到完成状态

        // 允许场景激活，跳转到目标场景
        operation.allowSceneActivation = true;
    }

    /// <summary>
    /// 静态方法，供其他脚本调用以开始加载指定场景。
    /// 会设置SceneToLoad并直接切换到LoadingScene。
    /// </summary>
    /// <param name="sceneName">要加载的目标场景名称</param>
    public static void LoadLevel(string sceneName)
    {
        SceneToLoad = sceneName;                 // 设置目标场景
        SceneManager.LoadScene("LoadingScene");  // 切换到加载场景
    }
}