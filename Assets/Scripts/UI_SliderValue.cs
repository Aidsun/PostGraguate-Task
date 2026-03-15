// 文件：UI_SliderValue.cs
// 模块：UI / 滑块数值显示
// 说明：该脚本通常作为Slider的子物体，用于将Slider的当前值实时显示在旁边的TextMeshPro文本上。
//      它自动查找父物体上的Slider组件，并监听其值变化事件，更新文本内容。
//      支持显示为百分比、自定义数字格式、前后缀等，适用于音量滑块、参数设置等场景。
// 特性：使用GetComponentInParent自动查找Slider，监听Slider.onValueChanged事件，
//      通过Tooltip、Header等特性优化Inspector配置。

using UnityEngine;
using UnityEngine.UI;          // 使用Slider组件
using TMPro;                   // 使用TextMeshPro文本组件

public class UI_SliderValue : MonoBehaviour
{
    [Header("绑定组件")]                     // Inspector分组
    [Tooltip("如果不拖，会自动查找父物体上的Slider")]
    public Slider targetSlider;              // 目标Slider组件，如果不拖拽则自动查找父物体

    [Tooltip("显示数值的文本框，如果不拖会自动查找")]
    public TMP_Text valueText;                // 显示数值的文本框，如果不拖拽则自动查找当前物体上的TMP_Text

    [Header("显示设置")]                       // 显示格式配置
    [Tooltip("是否显示为百分比? (例如 0.5 显示为 50%)")]
    public bool showPercent = false;          // 是否将数值显示为百分比（乘以100）

    [Tooltip("数字格式 (F0=整数, F1=1位小数, F2=2位小数)")]
    public string numberFormat = "F0";        // 数值格式化字符串，如"F0"表示无小数，"F1"表示1位小数

    [Tooltip("前缀 (例如 '音量: ')")]
    public string prefix = "";                // 显示在数值前的文本

    [Tooltip("后缀 (例如 '%')")]
    public string suffix = "";                // 显示在数值后的文本

    void Start()
    {
        // 1. 自动查找组件（如果未手动拖拽）
        // 在父物体中查找Slider组件
        if (targetSlider == null) targetSlider = GetComponentInParent<Slider>();
        // 在当前物体上查找TMP_Text组件
        if (valueText == null) valueText = GetComponent<TMP_Text>();

        // 2. 初始化监听
        if (targetSlider != null)
        {
            // 初始化显示一次（设置当前滑块值对应的文本）
            UpdateText(targetSlider.value);

            // 【关键修复】删掉了 RemoveAllListeners()
            // 这里不删除现有监听器，以确保不会移除其他脚本（如SettingPanel）已经绑定的逻辑。
            // 直接添加监听，允许同时存在多个监听函数。
            targetSlider.onValueChanged.AddListener(UpdateText);
        }
        else
        {
            // 如果未找到Slider，输出警告
            Debug.LogWarning($"UI_SliderValue: 在 {gameObject.name} 上没找到 Slider 组件！");
        }
    }

    /// <summary>
    /// 更新文本内容，根据滑块值进行格式化。
    /// 此方法被Slider的onValueChanged事件调用。
    /// </summary>
    /// <param name="val">滑块当前值</param>
    public void UpdateText(float val)
    {
        if (valueText == null) return;

        if (showPercent)
        {
            // 显示为百分比：乘以100并取整
            int percent = Mathf.RoundToInt(val * 100);
            valueText.text = $"{prefix}{percent}{suffix}";
        }
        else
        {
            // 按指定数字格式显示
            valueText.text = $"{prefix}{val.ToString(numberFormat)}{suffix}";
        }
    }

    /// <summary>
    /// 强制刷新文本（例如当Slider值被外部修改时手动调用）
    /// </summary>
    public void ForceRefresh()
    {
        if (targetSlider != null) UpdateText(targetSlider.value);
    }
}