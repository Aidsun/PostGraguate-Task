using UnityEngine;
using TMPro;

public class InteractionButton : MonoBehaviour
{
    public static InteractionButton Instance { get; private set; }

    [Header("UI组件绑定")]
    public GameObject interactionPanel;
    public TMP_Text interactionText;

    private void Awake()
    {
        // 标准单例写法
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 【强制隐藏】不管编辑器里是开是关，游戏开始必须关掉
        // 不要加 if 判断，直接关，最保险
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }

    // 显示提示
    public void ShowInteractionButton(string text)
    {
        if (interactionText != null)
        {
            interactionText.text = text;
        }

        // 只要面板存在，就显示出来
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
    }

    // 关闭提示
    public void HideInteractionButton()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}