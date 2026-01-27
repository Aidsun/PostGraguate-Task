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

    // === 打开面板 ===
    public void ShowPanel(string text)
    {
        contentText.text = text;
        panelObject.SetActive(true);

        // 【新增】播放音效
        PlayPanelSound();

        if (pauseGame)
        {
            Time.timeScale = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // === 关闭面板 (给按钮绑定) ===
    // 增加了一个可选参数 playSound，默认为 true。这样 Awake 初始化调用时可以填 false 不播声音
    public void HidePanel()
    {
        panelObject.SetActive(false);
        PlayBtnSound();

        if (pauseGame)
        {
            Time.timeScale = 1f;
        }

        // 只有当面板真的关闭了，才锁住鼠标
        // 防止还没看完就锁住了
        if (!panelObject.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // === 内部辅助方法：播放音效 ===
    private void PlayPanelSound()
    {
        // 检查所有引用是否安全
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.PanelOpenSound)
        {
            // 使用 UI 专用声道播放，且使用 PlayOneShot 防止打断其他声音
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.PanelOpenSound);
        }
    }
    private void PlayBtnSound()
    {
        // 检查所有引用是否安全
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.PanelOpenSound)
        {
            // 使用 UI 专用声道播放，且使用 PlayOneShot 防止打断其他声音
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
        }
    }
}