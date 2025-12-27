using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_SliderValue : MonoBehaviour
{
    [Header("绑定组件")]
    [Tooltip("如果不拖，会自动查找父物体上的Slider")]
    public Slider targetSlider;
    [Tooltip("显示数值的文本框，如果不拖会自动查找")]
    public TMP_Text valueText;

    [Header("显示设置")]
    [Tooltip("是否显示为百分比? (例如 0.5 显示为 50%)")]
    public bool showPercent = false;

    [Tooltip("数字格式 (F0=整数, F1=1位小数, F2=2位小数)")]
    public string numberFormat = "F0";

    [Tooltip("前缀 (例如 '音量: ')")]
    public string prefix = "";

    [Tooltip("后缀 (例如 '%')")]
    public string suffix = "";

    void Start()
    {
        // 1. 自动查找组件
        if (targetSlider == null) targetSlider = GetComponentInParent<Slider>();
        if (valueText == null) valueText = GetComponent<TMP_Text>();

        // 2. 初始化监听
        if (targetSlider != null)
        {
            // 初始化显示一次
            UpdateText(targetSlider.value);

            // 【关键修复】删掉了 RemoveAllListeners()
            // 这样就不会把 SettingPanel 绑定的音量控制逻辑给删掉了！
            targetSlider.onValueChanged.AddListener(UpdateText);
        }
        else
        {
            Debug.LogWarning($"UI_SliderValue: 在 {gameObject.name} 上没找到 Slider 组件！");
        }
    }

    public void UpdateText(float val)
    {
        if (valueText == null) return;

        if (showPercent)
        {
            int percent = Mathf.RoundToInt(val * 100);
            valueText.text = $"{prefix}{percent}{suffix}";
        }
        else
        {
            valueText.text = $"{prefix}{val.ToString(numberFormat)}{suffix}";
        }
    }

    public void ForceRefresh()
    {
        if (targetSlider != null) UpdateText(targetSlider.value);
    }
}