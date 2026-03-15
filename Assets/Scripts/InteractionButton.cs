// 文件：InteractionButton.cs
// 模块：UI / 交互提示按钮
// 说明：该脚本是一个单例UI管理器，负责显示和隐藏屏幕中央的交互提示面板。
//      当玩家指向可交互物体时，PlayerInteraction脚本会调用ShowInteractionButton
//      显示提示文字；当玩家离开物体时，调用HideInteractionButton隐藏面板。
// 特性：单例模式，Awake中强制隐藏面板，通过公共方法控制面板显示。

using UnityEngine;
using TMPro;               // 使用TextMeshPro文本组件

public class InteractionButton : MonoBehaviour
{
    // 单例实例，只读属性
    public static InteractionButton Instance { get; private set; }

    [Header("UI组件绑定")]   // 在Inspector中分组显示
    public GameObject interactionPanel;   // 交互提示面板的根物体（包含背景和文本）
    public TMP_Text interactionText;      // 用于显示交互提示文字的TextMeshPro组件

    private void Awake()
    {
        // 标准单例写法：如果不存在实例，则设置为当前实例；否则销毁当前对象
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 【强制隐藏】不管编辑器里是开是关，游戏开始必须关掉提示面板
        // 不添加任何条件判断，直接关闭，确保初始状态为隐藏
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示交互提示按钮，并设置提示文字。
    /// 由PlayerInteraction在玩家射线击中可交互物体时调用。
    /// </summary>
    /// <param name="text">要显示的提示文字，如“左键进行交互”</param>
    public void ShowInteractionButton(string text)
    {
        // 如果存在交互文本组件，则设置文本内容
        if (interactionText != null)
        {
            interactionText.text = text;
        }

        // 如果存在交互面板，则激活它（显示）
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏交互提示按钮。
    /// 由PlayerInteraction在玩家射线离开可交互物体时调用。
    /// </summary>
    public void HideInteractionButton()
    {
        // 如果存在交互面板，则取消激活（隐藏）
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}