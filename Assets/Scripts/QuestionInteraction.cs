using UnityEngine;

public class QuestionInteraction : MonoBehaviour
{
    [Header("可选：高亮边框")]
    public Renderer outlineRenderer;

    // 1. 被射线击中时高亮 (PlayerInteraction调用此方法)
    public void SetHighlight(bool active)
    {
        // 如果你有配置 outlineRenderer，这里会变色
        if (outlineRenderer && GameData.Instance)
        {
            outlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    // 2. 按 E 键交互 (PlayerInteraction调用此方法 - 名字必须叫 StartDisplay)
    public void StartDisplay()
    {
        Debug.Log("交互成功：请求打开答题面板");
        if (QuestionManager.Instance)
        {
            QuestionManager.Instance.openQuestionPanel();
        }
        else
        {
            Debug.LogError("场景中找不到 QuestionManager！请创建一个空物体挂载 QuestionManager 脚本。");
        }
    }
}