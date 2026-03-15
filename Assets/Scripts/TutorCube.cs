// 文件：TutorCube.cs
// 模块：交互对象 / 新手引导方块
// 说明：该脚本挂载在场景中的触发器物体（通常为透明立方体）上，用于实现新手引导提示。
//      当玩家（带有"Player"标签的对象）进入触发器时，会通过TutorPanel显示提示文本。
//      可选择是否只触发一次（oneTimeOnly），若是，则触发后将自己登记到GameData的完成列表，
//      并在后续场景加载时自动销毁（不再显示）。
// 特性：使用触发器（Collider）检测玩家，依赖TutorPanel单例显示提示，与GameData交互记录已触发的引导ID，
//      通过Header、Tooltip、TextArea优化Inspector面板。

using UnityEngine;

public class TutorCube : MonoBehaviour
{
    [Header("【核心设置】唯一标识符")]
    [Tooltip("请给每个方块起个不一样的名字，例如 Guide_Entrance, Guide_Hall")]
    public string guideID;                     // 引导方块的唯一标识，用于在GameData中记录是否已触发

    [Header("提示内容")]
    [TextArea(3, 10)]                           // 多行文本框，最小3行，最大10行
    public string myTipContent = "欢迎来到浏览馆！";   // 要显示的提示文本

    [Header("触发设置")]
    [Tooltip("勾选后，触发一次就永远消失（包括切场景回来也不再显示）")]
    public bool oneTimeOnly = true;              // 是否只触发一次，若为true则触发后永久消失

    private void Start()
    {
        // 1. 自动容错：如果你在 Inspector 忘了填 ID，就默认用物体名字
        if (string.IsNullOrEmpty(guideID))
        {
            guideID = gameObject.name;           // 使用物体名称作为ID
        }

        // 2. 【核心修复】出生时检查“防复活名单”
        // 如果该方块是只触发一次的，并且GameData中存在已完成ID列表
        if (oneTimeOnly && GameData.Instance != null)
        {
            // 如果名单里有我的名字，说明我已经死过一次了，立刻自杀（隐藏）
            if (GameData.Instance.CompletedGuideIds.Contains(guideID))
            {
                gameObject.SetActive(false);      // 隐藏该物体
            }
        }
    }

    // 当其他碰撞体进入触发器时调用
    private void OnTriggerEnter(Collider other)
    {
        // 判断进入的对象是否带有"Player"标签（通常是玩家）
        if (other.CompareTag("Player"))
        {
            // 3. 呼叫 UI：通过TutorPanel单例显示提示内容
            if (TutorPanel.Instance)
            {
                TutorPanel.Instance.ShowPanel(myTipContent);
            }

            // 4. 【核心修复】触发后，把自己登记到名单里
            if (oneTimeOnly)
            {
                if (GameData.Instance != null)
                {
                    // 防止重复添加（但触发器一般只会触发一次，这里做安全判断）
                    if (!GameData.Instance.CompletedGuideIds.Contains(guideID))
                    {
                        GameData.Instance.CompletedGuideIds.Add(guideID);   // 登记ID
                    }
                }

                // 任务完成，销毁自己（隐藏物体）
                gameObject.SetActive(false);
            }
        }
    }
}