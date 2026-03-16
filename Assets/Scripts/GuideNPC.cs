using UnityEngine;
using TMPro;

public class GuideNPC : MonoBehaviour
{
    [Header("导览员配置")]
    [TextArea(3, 5)]
    public string welcomeMessage = "欢迎来到红色阅兵展馆！为了让你更深入地了解历史，我们设置了7个答题点。每答对一题可以获得一枚印章，集齐全部7枚印章后，可以来找我领取奖励。";

    [TextArea(2, 3)]
    public string progressMessage = "你已经获得了 {0} / 7 枚印章，继续加油！"; // 使用占位符 {0} 替换实际数量

    [TextArea(3, 5)]
    public string rewardMessage = "恭喜你集齐了所有印章！这是给你的神秘奖励！";

    [TextArea(2, 3)]
    public string alreadyClaimedMessage = "你已经领取过奖励了，谢谢参与！";

    [Header("奖励设置")]
    public GameObject rewardEffect;      // 奖励特效预制体（可选）
    public AudioClip rewardSound;        // 奖励音效（可选）

    [Header("组件绑定")]
    public Renderer outlineRenderer;      // 高亮边框
    public TMP_Text nameLabel;            // 头顶名字（可选）

    private bool hasIntroduced = false;   // 是否已经介绍过任务
    private const int totalStamps = 7;    // 总印章数（可与答题点数量保持一致）

    private void Start()
    {
        if (nameLabel != null)
            nameLabel.text = "导览员";
    }

    // 由 PlayerInteraction 调用，设置高亮
    public void SetHighlight(bool active)
    {
        if (outlineRenderer != null && GameData.Instance != null)
        {
            outlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    // 由 PlayerInteraction 调用，触发对话
    public void StartDisplay()
    {
        if (GameData.Instance == null) return;
        if (TutorPanel.Instance == null) return;

        int collected = GameData.Instance.collectedStamps.Count;

        // 如果已经集齐印章
        if (collected >= totalStamps)
        {
            // 如果尚未领取奖励，则触发奖励
            if (!GameData.Instance.rewardClaimed)
            {
                GiveReward();
            }
            else
            {
                // 已领取过奖励，显示提示
                TutorPanel.Instance.ShowPanel(alreadyClaimedMessage);
            }
            return;
        }

        // 未集齐印章
        if (!hasIntroduced)
        {
            // 首次对话：介绍任务
            TutorPanel.Instance.ShowPanel(welcomeMessage);
            hasIntroduced = true;
        }
        else
        {
            // 非首次对话：报告进度
            string progress = string.Format(progressMessage, collected);
            TutorPanel.Instance.ShowPanel(progress);
        }
    }

    // 发放奖励
    private void GiveReward()
    {
        // 显示奖励信息
        TutorPanel.Instance.ShowPanel(rewardMessage);

        // 播放奖励特效（如果有）
        if (rewardEffect != null)
        {
            Instantiate(rewardEffect, transform.position, Quaternion.identity);
        }

        // 播放奖励音效（建议使用 BtnSource 或专门的 SFX 源）
        if (rewardSound != null && AudioManager.Instance != null && AudioManager.Instance.BtnSource != null)
        {
            AudioManager.Instance.BtnSource.PlayOneShot(rewardSound);
        }

        // 标记已领取奖励（需要在 GameData 中添加 rewardClaimed 字段）
        GameData.Instance.rewardClaimed = true;
    }
}