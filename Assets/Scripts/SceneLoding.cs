using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SceneLoding : MonoBehaviour
{
    [Header("UI组件")]
    public Slider progressBar;
    public TMP_Text progressText;

    // 静态变量：要加载的目标场景名字
    public static string SceneToLoad;

    [Header("设置")]
    [Tooltip("最小加载时间（秒），防止加载太快玩家看不清提示")]
    [Range(1, 10)]
    public float minLoadTime = 3.0f; // 建议改成 3秒，5秒有点太久了

    void Start()
    {
        if (!string.IsNullOrEmpty(SceneToLoad))
        {
            StartCoroutine(LoadAsync(SceneToLoad));
        }
    }

    IEnumerator LoadAsync(string sceneName)
    {
        // 1. 尝试异步加载场景
        AsyncOperation operation = null;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载报错: {e.Message}");
        }

        // 【关键修复】安全气囊：如果场景没在 Build Settings 里，operation 会是 null
        if (operation == null)
        {
            string errorMsg = $"【严重错误】无法加载场景 '{sceneName}'！\n请检查：\n1. 场景名字拼写是否正确。\n2. 该场景是否已添加到 File -> Build Settings 列表中！";
            Debug.LogError(errorMsg);

            if (progressText) progressText.text = "加载失败: 场景未找到 (看控制台)";
            yield break; // 强制退出协程，防止后面报错崩溃
        }

        // 暂时不让场景自动跳转
        operation.allowSceneActivation = false;

        float timer = 0f;

        // 2. 循环等待
        // 条件：(进度没满 0.9) 或者 (时间没到 minLoadTime)
        while (operation.progress < 0.9f || timer < minLoadTime)
        {
            timer += Time.deltaTime;

            // 计算真实的加载进度 (0 ~ 1)
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // 计算时间的进度 (0 ~ 1)
            float timeProgress = Mathf.Clamp01(timer / minLoadTime);

            // 【流畅度技巧】取两者中较小的那个，这样进度条永远不会比时间跑得快
            float finalDisplayProgress = Mathf.Min(loadProgress, timeProgress);

            // 更新UI
            if (progressBar) progressBar.value = finalDisplayProgress;
            if (progressText)
                progressText.text = $"正在前往目的地... {(finalDisplayProgress * 100):F0}%";

            yield return null;
        }

        // 3. 加载完成，最后冲刺
        if (progressBar) progressBar.value = 1;
        if (progressText) progressText.text = "准备就绪! 100%";

        // 稍微停顿一下，让玩家看到 100%
        yield return new WaitForSeconds(0.2f);

        // 放行，允许跳转
        operation.allowSceneActivation = true;
    }

    // 静态方法：供其他脚本（如 SettingPanel, StartGame）调用
    public static void LoadLevel(string sceneName)
    {
        SceneToLoad = sceneName;
        // 确保你的 Loading 场景名字叫 "LoadingScene"，且已加入 Build Settings
        SceneManager.LoadScene("LoadingScene");
    }
}