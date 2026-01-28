using UnityEngine;

public class TutorCube : MonoBehaviour
{
    [Header("【核心设置】唯一标识符")]
    [Tooltip("请给每个方块起个不一样的名字，例如 Guide_Entrance, Guide_Hall")]
    public string guideID;

    [Header("提示内容")]
    [TextArea(3, 10)]
    public string myTipContent = "欢迎来到浏览馆！";

    [Header("触发设置")]
    [Tooltip("勾选后，触发一次就永远消失（包括切场景回来也不再显示）")]
    public bool oneTimeOnly = true;

    private void Start()
    {
        // 1. 自动容错：如果你在 Inspector 忘了填 ID，就默认用物体名字
        if (string.IsNullOrEmpty(guideID))
        {
            guideID = gameObject.name;
        }

        // 2. 【核心修复】出生时检查“防复活名单”
        if (oneTimeOnly && GameData.Instance != null)
        {
            // 如果名单里有我的名字，说明我已经死过一次了，立刻自杀
            if (GameData.Instance.CompletedGuideIds.Contains(guideID))
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 3. 呼叫 UI
            if (TutorPanel.Instance)
            {
                TutorPanel.Instance.ShowPanel(myTipContent);
            }

            // 4. 【核心修复】触发后，把自己登记到名单里
            if (oneTimeOnly)
            {
                if (GameData.Instance != null)
                {
                    // 防止重复添加
                    if (!GameData.Instance.CompletedGuideIds.Contains(guideID))
                    {
                        GameData.Instance.CompletedGuideIds.Add(guideID);
                    }
                }

                // 任务完成，销毁自己
                gameObject.SetActive(false);
            }
        }
    }
}