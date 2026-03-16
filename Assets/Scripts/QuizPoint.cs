// 文件：QuizPoint.cs
// 模块：交互对象 / 答题点
// 说明：每个答题点对应一枚印章，玩家答题一次（随机抽7题），正确率≥80%获得印章。
//       无论是否通过，都不能再次答题。

using UnityEngine;
using TMPro;

public class QuizPoint : MonoBehaviour
{
    [Header("答题点配置")]
    [Tooltip("该点对应的印章ID，必须唯一")]
    public string stampId;

    [Header("组件")]
    public Renderer outlineRenderer;   // 高亮边框（可选）

    private bool hasAttempted = false; // 是否已经尝试过（无论成败）

    private void Start()
    {
        // 如果已经获得过该印章，则标记为已尝试
        if (GameData.Instance != null && GameData.Instance.collectedStamps.Contains(stampId))
        {
            hasAttempted = true;
        }
    }

    // 由 PlayerInteraction 调用，设置高亮
    public void SetHighlight(bool active)
    {
        if (outlineRenderer != null && GameData.Instance != null)
        {
            outlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    // 由 PlayerInteraction 调用，触发答题
    public void StartDisplay()
    {
        if (GameData.Instance == null) return;

        // 情况1：已经获得该印章
        if (GameData.Instance.collectedStamps.Contains(stampId))
        {
            TutorPanel.Instance?.ShowPanel("你已经获得了这里的印章，请去其他答题点吧。");
            return;
        }

        // 情况2：之前尝试过但未通过
        if (hasAttempted)
        {
            TutorPanel.Instance?.ShowPanel("你之前未通过考核，无法再次答题。");
            return;
        }

        // 情况3：未尝试过，开始答题
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.StartRandomRound(OnRoundFinished);
        }
        else
        {
            Debug.LogError("场景中找不到 QuestionManager！");
        }
    }

    private void OnRoundFinished(int correctCount)
    {
        const int totalQuestions = 7;
        float rate = (float)correctCount / totalQuestions;
        bool passed = rate >= 0.6f;

        Debug.Log($"[QuizPoint] 答题结束，正确数：{correctCount}，通过：{passed}");

        // 检查 TutorPanel 实例
        if (TutorPanel.Instance == null)
        {
            Debug.LogError("[QuizPoint] TutorPanel.Instance 为 null！");
            return;
        }
        Debug.Log("[QuizPoint] TutorPanel.Instance 存在，准备显示面板");

        string message;
        if (passed)
        {
            GameData.Instance.collectedStamps.Add(stampId);
            message = $"恭喜你答对了{correctCount}题，通过考核！获得印章一枚。";
            Debug.Log($"[QuizPoint] 获得印章: {stampId}");
        }
        else
        {
            message = $"很遗憾，你只答对了{correctCount}题，未达到80%，无法获得印章。";
        }

        TutorPanel.Instance.ShowPanel(message);
        hasAttempted = true;
    }
}