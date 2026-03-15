// 文件：QuestionInteraction.cs
// 模块：交互对象 / 答题点
// 说明：该脚本挂载在主馆场景中的答题点物体上，负责答题点的交互逻辑。
//      它实现了两个方法：SetHighlight（用于高亮反馈）和 StartDisplay（用于触发答题面板）。
//      这些方法由 PlayerInteraction 通过 SendMessage 调用，实现了解耦的交互机制。
// 特性：通过 SendMessage 被调用，依赖 QuestionManager 单例来打开答题面板。

using UnityEngine;

public class QuestionInteraction : MonoBehaviour
{
    [Header("可选：高亮边框")]        // 在Inspector中分组显示
    // 可选的高亮边框渲染器，如果提供了该组件，则会在高亮时改变其颜色
    public Renderer outlineRenderer;

    /// <summary>
    /// 设置高亮状态，由 PlayerInteraction 通过 SendMessage 调用。
    /// 当玩家射线击中该物体时，会调用此方法并传入 true；离开时传入 false。
    /// </summary>
    /// <param name="active">是否激活高亮</param>
    public void SetHighlight(bool active)
    {
        // 如果配置了 outlineRenderer 且 GameData 实例存在，则根据 active 改变颜色
        if (outlineRenderer && GameData.Instance)
        {
            // 高亮时使用 GameData 中配置的高亮颜色，否则恢复白色
            outlineRenderer.material.color = active ? GameData.Instance.HighlightColor : Color.white;
        }
    }

    /// <summary>
    /// 开始交互，由 PlayerInteraction 在玩家点击时通过 SendMessage 调用（方法名必须为 StartDisplay）。
    /// 该方法负责打开答题面板。
    /// </summary>
    public void StartDisplay()
    {
        Debug.Log("交互成功：请求打开答题面板");

        // 通过 QuestionManager 单例打开答题面板
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