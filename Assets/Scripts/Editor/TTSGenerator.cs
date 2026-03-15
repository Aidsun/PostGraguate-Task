// 文件：TTSGenerator.cs
// 模块：编辑器工具 / 语音生成
// 说明：该脚本提供了在Unity编辑器中调用Edge-TTS生成语音的功能，并自动将生成的音频文件导入项目。
//      包含核心逻辑类TTSCore，以及为ImageExhibition、VideoExhibition、PanoramaExhibition提供的自定义Inspector界面。
// 特性：使用UnityEditor命名空间，仅在编辑器下编译；使用了async/await异步编程；利用外部进程调用Edge-TTS。

using UnityEngine;
using UnityEditor;          // 编辑器功能命名空间，用于扩展Inspector
using System.Diagnostics;   // 用于启动外部进程
using System.IO;            // 文件操作
using System.Threading.Tasks; // 异步任务

// =========================================================
// 第一部分：核心逻辑引擎 (TTSCore) - 保持不变
// =========================================================

/// <summary>
/// TTS核心逻辑类，提供音色选择、UI绘制、音频生成等功能。
/// 该类为静态类，无需实例化，可直接调用其方法。
/// </summary>
public static class TTSCore
{
    // 音色显示名称数组（用于下拉菜单）
    public static string[] voiceDisplayNames = new string[] { "晓晓 (女)", "云希 (男)", "云扬 (男)", "晓涵 (女)", "晓墨 (女)", "云夏 (男)", "晓睿 (女)", "云健 (男)", "东北老铁" };

    // 对应的Edge-TTS语音ID数组，与显示名称一一对应
    public static string[] voiceIds = new string[] { "zh-CN-XiaoxiaoNeural", "zh-CN-YunxiNeural", "zh-CN-YunyangNeural", "zh-CN-XiaohanNeural", "zh-CN-XiaomoNeural", "zh-CN-YunxiaNeural", "zh-CN-XiaoruiNeural", "zh-CN-YunjianNeural", "zh-CN-liaoning-XiaobeiNeural" };

    /// <summary>
    /// 在Inspector中绘制TTS生成GUI。
    /// 包含音色下拉选择框和生成按钮。
    /// </summary>
    /// <param name="title">展品标题，用于生成文件名</param>
    /// <param name="descriptionText">描述文本，将转换为语音</param>
    /// <param name="selectedVoiceIndex">当前选中的音色索引</param>
    /// <param name="onVoiceChanged">音色改变时的回调</param>
    /// <param name="onGenerateClick">点击生成按钮时的回调</param>
    public static void DrawTTSGUI(string title, string descriptionText, int selectedVoiceIndex, System.Action<int> onVoiceChanged, System.Action onGenerateClick)
    {
        // 添加一些垂直间距
        GUILayout.Space(20);
        // 显示一个标题标签，使用编辑器粗体样式
        GUILayout.Label("🎙️ RedGenie 语音生成", EditorStyles.boldLabel);
        // 绘制音色选择下拉框，返回新选择的索引
        int newIndex = EditorGUILayout.Popup("选择音色", selectedVoiceIndex, voiceDisplayNames);
        // 如果索引改变，调用回调
        if (newIndex != selectedVoiceIndex) onVoiceChanged(newIndex);

        // 绘制生成按钮，高度40像素
        if (GUILayout.Button("生成/更新 配音", GUILayout.Height(40)))
        {
            // 如果描述文本为空，弹出错误对话框并返回
            if (string.IsNullOrEmpty(descriptionText)) { EditorUtility.DisplayDialog("错误", "描述文本为空！", "OK"); return; }
            // 否则调用生成回调
            onGenerateClick();
        }
    }

    /// <summary>
    /// 异步生成音频文件，并返回生成的AudioClip。
    /// 调用外部Edge-TTS命令行工具生成MP3文件，然后导入到Unity项目中。
    /// </summary>
    /// <param name="title">标题，用于生成文件名</param>
    /// <param name="text">要转换为语音的文本</param>
    /// <param name="voiceIndex">选中的音色索引</param>
    /// <param name="onComplete">生成完成后的回调，参数为生成的AudioClip</param>
    public static async void GenerateAudio(string title, string text, int voiceIndex, System.Action<AudioClip> onComplete)
    {
        // 替换文本中的换行符和双引号，避免命令行参数错误
        text = text.Replace("\n", " ").Replace("\"", "“");
        // 获取选中的语音ID
        string voice = voiceIds[voiceIndex];
        // 定义保存音频的文件夹路径：项目/Assets/Resources/Audio/TTS
        string folderPath = Application.dataPath + "/Resources/Audio/TTS";
        // 如果文件夹不存在，则创建
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // 生成文件名：标题_语音ID.mp3
        string fileName = $"{title}_{voice}.mp3";
        // 完整文件路径
        string fullPath = Path.Combine(folderPath, fileName);
        // Unity资源路径（相对Assets文件夹）
        string assetPath = $"Assets/Resources/Audio/TTS/{fileName}";

        // 显示进度条
        EditorUtility.DisplayProgressBar("生成中", "正在连接 Edge-TTS...", 0.5f);
        // 异步运行Edge-TTS命令
        bool success = await RunEdgeTTS(text, fullPath, voice);
        // 关闭进度条
        EditorUtility.ClearProgressBar();

        if (success)
        {
            // 刷新资源数据库，让新文件出现在Project窗口
            AssetDatabase.Refresh();
            // 从资源路径加载生成的AudioClip
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null)
            {
                // 调用完成回调，传递clip
                onComplete(clip);
                UnityEngine.Debug.Log($"✅ 成功: {fileName}");
            }
        }
        else
        {
            // 弹出失败对话框
            EditorUtility.DisplayDialog("失败", "请确保已安装 Python 和 edge-tts", "OK");
        }
    }

    /// <summary>
    /// 异步运行Edge-TTS命令，生成MP3文件。
    /// 使用Task.Run在后台线程执行，避免阻塞主线程。
    /// </summary>
    /// <param name="text">要转换的文本</param>
    /// <param name="outputPath">输出MP3文件路径</param>
    /// <param name="voice">语音ID</param>
    /// <returns>是否成功</returns>
    private static async Task<bool> RunEdgeTTS(string text, string outputPath, string voice)
    {
        return await Task.Run(() =>
        {
            try
            {
                // 创建新进程
                Process p = new Process();
                // 设置启动信息
                p.StartInfo.FileName = "edge-tts";   // 命令行程序名（需在PATH中）
                p.StartInfo.Arguments = $"--text \"{text}\" --write-media \"{outputPath}\" --voice {voice}";
                p.StartInfo.UseShellExecute = false; // 不使用系统Shell
                p.StartInfo.CreateNoWindow = true;    // 不创建窗口
                p.Start();                             // 启动进程
                p.WaitForExit();                        // 等待进程结束
                return p.ExitCode == 0;                 // 返回是否成功（退出码0表示成功）
            }
            catch { return false; }
        });
    }
}

// =========================================================
// 第二部分：适配新版 ImageExhibition (变量名已改为 Title, Description)
// =========================================================

/// <summary>
/// ImageExhibition的自定义Inspector扩展，在Inspector中添加TTS生成按钮。
/// 当EnableVoice为true时，显示音色选择和生成按钮，点击后生成语音并自动赋值给VoiceClip。
/// </summary>
[CustomEditor(typeof(ImageExhibition))]   // 指定该编辑器扩展应用于ImageExhibition类型
public class ImageTTSGenerator : Editor
{
    private int selectedVoiceIndex = 0;    // 记忆当前选择的音色索引

    public override void OnInspectorGUI()
    {
        // 先绘制默认的Inspector（显示所有原有字段）
        DrawDefaultInspector();
        // 获取当前正在编辑的目标对象
        ImageExhibition script = (ImageExhibition)target;

        // 如果启用了语音（EnableVoice为true），则显示TTS生成界面
        if (script.EnableVoice)
        {
            // 调用核心类的绘制方法，传入标题、描述、当前音色索引，以及回调
            TTSCore.DrawTTSGUI(script.Title, script.Description, selectedVoiceIndex,
                (index) => selectedVoiceIndex = index,   // 音色改变时更新本地索引
                () => {
                    // 点击生成按钮时调用GenerateAudio
                    TTSCore.GenerateAudio(script.Title, script.Description, selectedVoiceIndex, (clip) => {
                        // 生成完成后，将生成的AudioClip赋值给脚本的VoiceClip字段
                        script.VoiceClip = clip;
                        // 标记脚本为脏，使Inspector保存修改
                        EditorUtility.SetDirty(script);
                    });
                });
        }
    }
}

// =========================================================
// 第三部分：适配新版 VideoExhibition
// =========================================================

/// <summary>
/// VideoExhibition的自定义Inspector扩展，与ImageTTSGenerator类似。
/// </summary>
[CustomEditor(typeof(VideoExhibition))]
public class VideoTTSGenerator : Editor
{
    private int selectedVoiceIndex = 0;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VideoExhibition script = (VideoExhibition)target;

        if (script.EnableVoice)
        {
            TTSCore.DrawTTSGUI(script.Title, script.Description, selectedVoiceIndex,
                (index) => selectedVoiceIndex = index,
                () => {
                    TTSCore.GenerateAudio(script.Title, script.Description, selectedVoiceIndex, (clip) => {
                        script.VoiceClip = clip;
                        EditorUtility.SetDirty(script);
                    });
                });
        }
    }
}

// =========================================================
// 第四部分：适配新版 PanoramaExhibition
// =========================================================

/// <summary>
/// PanoramaExhibition的自定义Inspector扩展。
/// 注意：PanoramaExhibition使用DescriptionNote作为描述文本，而不是Description。
/// </summary>
[CustomEditor(typeof(PanoramaExhibition))]
public class PanoramaTTSGenerator : Editor
{
    private int selectedVoiceIndex = 0;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PanoramaExhibition script = (PanoramaExhibition)target;

        // 全景展品使用DescriptionNote作为描述文本（因为全景通常不需要在展示时显示长文本，DescriptionNote仅供编辑器使用）
        if (script.EnableVoice)
        {
            TTSCore.DrawTTSGUI(script.Title, script.DescriptionNote, selectedVoiceIndex,
                (index) => selectedVoiceIndex = index,
                () => {
                    TTSCore.GenerateAudio(script.Title, script.DescriptionNote, selectedVoiceIndex, (clip) => {
                        script.VoiceClip = clip;
                        EditorUtility.SetDirty(script);
                    });
                });
        }
    }
}