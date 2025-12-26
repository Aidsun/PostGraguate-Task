using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

// =========================================================
// 第一部分：核心逻辑引擎 (TTSCore) - 保持不变
// =========================================================
public static class TTSCore
{
    public static string[] voiceDisplayNames = new string[] { "晓晓 (女)", "云希 (男)", "云扬 (男)", "晓涵 (女)", "晓墨 (女)", "云夏 (男)", "晓睿 (女)", "云健 (男)", "东北老铁" };
    public static string[] voiceIds = new string[] { "zh-CN-XiaoxiaoNeural", "zh-CN-YunxiNeural", "zh-CN-YunyangNeural", "zh-CN-XiaohanNeural", "zh-CN-XiaomoNeural", "zh-CN-YunxiaNeural", "zh-CN-XiaoruiNeural", "zh-CN-YunjianNeural", "zh-CN-liaoning-XiaobeiNeural" };

    public static void DrawTTSGUI(string title, string descriptionText, int selectedVoiceIndex, System.Action<int> onVoiceChanged, System.Action onGenerateClick)
    {
        GUILayout.Space(20);
        GUILayout.Label("🎙️ RedGenie 语音生成", EditorStyles.boldLabel);
        int newIndex = EditorGUILayout.Popup("选择音色", selectedVoiceIndex, voiceDisplayNames);
        if (newIndex != selectedVoiceIndex) onVoiceChanged(newIndex);

        if (GUILayout.Button("生成/更新 配音", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(descriptionText)) { EditorUtility.DisplayDialog("错误", "描述文本为空！", "OK"); return; }
            onGenerateClick();
        }
    }

    public static async void GenerateAudio(string title, string text, int voiceIndex, System.Action<AudioClip> onComplete)
    {
        text = text.Replace("\n", " ").Replace("\"", "“");
        string voice = voiceIds[voiceIndex];
        string folderPath = Application.dataPath + "/Resources/Audio/TTS";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string fileName = $"{title}_{voice}.mp3";
        string fullPath = Path.Combine(folderPath, fileName);
        string assetPath = $"Assets/Resources/Audio/TTS/{fileName}";

        EditorUtility.DisplayProgressBar("生成中", "正在连接 Edge-TTS...", 0.5f);
        bool success = await RunEdgeTTS(text, fullPath, voice);
        EditorUtility.ClearProgressBar();

        if (success)
        {
            AssetDatabase.Refresh();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null) { onComplete(clip); UnityEngine.Debug.Log($"✅ 成功: {fileName}"); }
        }
        else { EditorUtility.DisplayDialog("失败", "请确保已安装 Python 和 edge-tts", "OK"); }
    }

    private static async Task<bool> RunEdgeTTS(string text, string outputPath, string voice)
    {
        return await Task.Run(() =>
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "edge-tts";
                p.StartInfo.Arguments = $"--text \"{text}\" --write-media \"{outputPath}\" --voice {voice}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch { return false; }
        });
    }
}

// =========================================================
// 第二部分：适配新版 ImageExhibition (变量名已改为 Title, Description)
// =========================================================
[CustomEditor(typeof(ImageExhibition))]
public class ImageTTSGenerator : Editor
{
    private int selectedVoiceIndex = 0;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ImageExhibition script = (ImageExhibition)target;

        if (script.EnableVoice) // 变量名变了：enableVoiceover -> EnableVoice
        {
            TTSCore.DrawTTSGUI(script.Title, script.Description, selectedVoiceIndex,
                (index) => selectedVoiceIndex = index,
                () => {
                    TTSCore.GenerateAudio(script.Title, script.Description, selectedVoiceIndex, (clip) => {
                        script.VoiceClip = clip; // 变量名变了：artAudioClip -> VoiceClip
                        EditorUtility.SetDirty(script);
                    });
                });
        }
    }
}

// =========================================================
// 第三部分：适配新版 VideoExhibition
// =========================================================
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
[CustomEditor(typeof(PanoramaExhibition))]
public class PanoramaTTSGenerator : Editor
{
    private int selectedVoiceIndex = 0;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PanoramaExhibition script = (PanoramaExhibition)target;

        // 全景现在使用 DescriptionNote (编辑器专用描述)
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