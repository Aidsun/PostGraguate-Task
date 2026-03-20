using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GuideNPC : MonoBehaviour
{
    [Header("导览员配置")]
    [TextArea(3, 5)]
    public string welcomeMessage = "欢迎来到红色阅兵展馆！为了让你更深入地了解历史，我们设置了7个答题点。每答对一题可以获得一枚印章，集齐全部7枚印章后，可以来找我领取奖励。";

    [TextArea(2, 3)]
    public string progressMessage = "你已经获得了 {0} / 7 枚印章，继续加油！";

    [TextArea(3, 5)]
    public string rewardMessage = "恭喜你集齐了所有印章！获得称号【绝对老手】！";

    [TextArea(2, 3)]
    public string alreadyClaimedMessage = "你已经领取过奖励了，谢谢参与！";

    [Header("引导设置")]
    public GameObject routeArrow;  // 路线指示箭头物体，第一次对话后消失

    [Header("开发者模式（测试用）")]
    public bool devFastReward = false;

    [Header("组件绑定")]
    public Renderer outlineRenderer;
    public TMP_Text nameLabel;

    private const int totalStamps = 7;

    private void Start()
    {
        if (nameLabel != null)
            nameLabel.text = "导览员";

        // 如果任务已经解锁（例如加载存档），则隐藏箭头
        //if (GameData.Instance != null && GameData.Instance.questStarted && routeArrow != null)
        //    routeArrow.SetActive(false);
    }

    public void SetHighlight(bool active)
    {
        if (outlineRenderer != null && GameData.Instance != null)
        {
            outlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    public void StartDisplay()
    {
        if (GameData.Instance == null || TutorPanel.Instance == null) return;

        int collected = GameData.Instance.collectedStamps.Count;

        // 已领取奖励
        if (GameData.Instance.rewardClaimed)
        {
            TutorPanel.Instance.ShowPanel(alreadyClaimedMessage);
            return;
        }

        // 开发者模式：已介绍任务且未集齐时直接给奖励
        if (devFastReward && GameData.Instance.questStarted && collected < totalStamps)
        {
            GiveReward();
            return;
        }

        // 集齐印章
        if (collected >= totalStamps)
        {
            GiveReward();
            return;
        }

        // 未集齐且未领取
        if (!GameData.Instance.questStarted)
        {
            TutorPanel.Instance.ShowPanel(welcomeMessage);
            GameData.Instance.questStarted = true;
            GameData.Instance.SaveGame();

            // 隐藏路线箭头
            //if (routeArrow != null)
            //    routeArrow.SetActive(false);
        }
        else
        {
            string progress = string.Format(progressMessage, collected);
            TutorPanel.Instance.ShowPanel(progress);
        }
    }

    private void GiveReward()
    {
        TutorPanel.Instance.ShowPanel(rewardMessage);
        GameData.Instance.rewardClaimed = true;
        GameData.Instance.SaveGame();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GuideNPC))]
public class GuideNPCInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("重置收集进度"))
        {
            if (GameData.Instance != null)
            {
                GameData.Instance.ResetProgress();
                Debug.Log("收集进度已重置");
            }
            else
            {
                Debug.LogWarning("GameData.Instance 不存在");
            }
        }
    }
}
#endif