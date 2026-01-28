using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorPanel : MonoBehaviour
{
    // 单例模式
    public static TutorPanel Instance { get; private set; }

    [Header("UI 组件")]
    public GameObject panelObject;      // 面板物体
    public TextMeshProUGUI contentText; // 文本框

    [Header("设置")]
    public bool pauseGame = false;       // 是否暂停游戏

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HidePanel(); // 初始化时隐藏
    }

    // =========================================================
    // 【核心优化】新增 Update 方法
    // 作用：实时保护鼠标状态。只要面板开着，就强制鼠标显示。
    // 这能完美解决“关闭设置面板后，提示面板还在，但鼠标没了”的Bug。
    // =========================================================
    private void Update()
    {
        if (panelObject != null && panelObject.activeSelf)
        {
            // 如果发现鼠标被别人偷偷藏起来了，或者锁住了
            if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
            {
                // 立刻强制恢复显示！
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    // === 打开面板 ===
    public void ShowPanel(string text)
    {
        contentText.text = text;
        panelObject.SetActive(true);

        PlayPanelSound();

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        // 虽然 Update 会做，但打开瞬间也做一次，响应更快
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // === 关闭面板 ===
    public void HidePanel()
    {
        panelObject.SetActive(false);
        PlayBtnSound();

        if (pauseGame)
        {
            Time.timeScale = 1f;
        }

        // 只有当面板真的关闭了，才锁住鼠标
        if (!panelObject.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // === 内部辅助方法 ===
    private void PlayPanelSound()
    {
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.PanelOpenSound)
        {
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.PanelOpenSound);
        }
    }
    private void PlayBtnSound()
    {
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.PanelOpenSound)
        {
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
        }
    }
}