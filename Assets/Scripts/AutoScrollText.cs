// 文件：AutoScrollText.cs
// 模块：UI / 跑马灯文本
// 说明：该脚本用于实现文本自动滚动（跑马灯）效果。它根据给定的时长计算滚动速度，
//      使文本从右侧进入，左侧退出，常用于视频或图片展示时的滚动字幕。
// 特性：依赖TextMeshPro组件，通过RectTransform控制位置，使用Update逐帧移动。

using UnityEngine;
using TMPro;  // 使用TextMeshPro命名空间，用于文本处理

public class AutoScrollText : MonoBehaviour
{
    // 私有变量：该文本的RectTransform组件，用于控制位置和尺寸
    private RectTransform textRect;
    // 私有变量：该文本的TMP_Text组件，用于获取文本宽度等信息
    private TMP_Text tmpText;
    // 私有变量：文本自身宽度（以像素为单位）
    private float textWidth;
    // 私有变量：父物体的宽度（即遮罩条的宽度）
    private float parentWidth;

    // 私有变量：计算出的滚动速度（单位：像素/秒）
    private float calculatedSpeed = 0f;
    // 私有变量：是否正在滚动
    private bool isScrolling = false;

    // Awake在脚本实例化时调用，用于获取组件引用
    void Awake()
    {
        // 获取当前物体上的RectTransform组件
        textRect = GetComponent<RectTransform>();
        // 获取当前物体上的TMP_Text组件
        tmpText = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 根据给定的时长，自动计算速度并开始滚动
    /// </summary>
    /// <param name="duration">解说音频的时长(秒)</param>
    public void StartScrollingByDuration(float duration)
    {
        // 如果必要组件为空，则直接返回，避免空引用异常
        if (textRect == null || tmpText == null) return;

        // 1. 强制刷新TextMeshPro的网格，确保获取到精准的文字宽度
        //    因为preferredWidth可能需要在网格更新后才能得到准确值
        tmpText.ForceMeshUpdate();
        textWidth = tmpText.preferredWidth;  // 获取文本的完整宽度（包括所有字符）

        // 2. 获取父物体（遮罩条）的RectTransform，并获取其宽度
        RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
        parentWidth = parentRect.rect.width;  // 父物体的宽度（即显示区域的宽度）

        // 3. 设置初始位置：
        //    要求：开始时，文本的第一个字出现在右边 -> 即文本整体在遮罩右侧外
        //    假设Text的Pivot X是0（左对齐），因此anchoredPosition的x坐标设为父物体宽度
        textRect.anchoredPosition = new Vector2(parentWidth, 0);

        // 4. 计算速度：
        //    要求：结束时，最后一个字刚好出现在屏幕右边。
        //    起点：文本左边对齐屏幕右边 (X = parentWidth)
        //    终点：文本右边对齐屏幕右边 (X = parentWidth - textWidth)
        //    因此，文本需要向左移动的距离 = 文本自身的长度 (textWidth)
        float distance = textWidth;

        if (duration > 0)
        {
            // 根据距离和时长计算速度
            calculatedSpeed = distance / duration;
        }
        else
        {
            // 如果没有提供有效的音频时长（如为0或负数），则使用默认速度100像素/秒
            calculatedSpeed = 100f;
        }

        // 开始滚动
        isScrolling = true;
        // 输出调试信息，方便查看计算出的文本长度、时长和速度
        Debug.Log($"[跑马灯] 文本长度:{textWidth}, 目标时长:{duration}s, 计算速度:{calculatedSpeed}");
    }

    // Update每帧调用一次，用于移动文本位置
    void Update()
    {
        // 如果当前没有在滚动，则跳过
        if (!isScrolling) return;

        // 向左移动：每帧减少x坐标，移动距离为速度乘以时间增量
        textRect.anchoredPosition += Vector2.left * calculatedSpeed * Time.deltaTime;

        // 循环逻辑保护：如果为了防止文字跑太远找不到了，可以加个重置
        // 当文字完全跑出左边屏幕时 (Pos X < -textWidth)
        // 当前代码中注释掉了重置逻辑，因此文字会一直向左移动直到超出屏幕后停止
        // 你可以根据需要启用重置逻辑，实现循环滚动
        if (textRect.anchoredPosition.x < -textWidth)
        {
            // 停止或者重置? 这里让它循环，或者你可以选择直接 isScrolling = false;
            // textRect.anchoredPosition = new Vector2(parentWidth, 0);
        }
    }
}