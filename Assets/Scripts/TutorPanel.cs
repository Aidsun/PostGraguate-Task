// 文件：TutorPanel.cs
// 模块：UI / 新手引导提示面板
// 说明：该脚本是一个全局单例的UI管理器，负责显示和隐藏新手引导提示面板。
//      当玩家触发TutorCube时，TutorPanel会显示提示文本，并可选择是否暂停游戏。
//      它实时监控鼠标状态，确保面板打开时鼠标始终可见且未锁定，避免与其他UI（如设置面板）冲突。
// 特性：单例模式，通过Update实时强制鼠标显示，使用协程（虽然这里未使用），
//      与AudioManager交互播放音效，与GameData交互获取音效资源。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;                  // 使用TextMeshPro文本组件
using UnityEngine.UI;         // 使用UI组件（虽未直接使用Button，但保留了引用）

public class TutorPanel : MonoBehaviour
{
    // 单例实例，只读属性
    public static TutorPanel Instance { get; private set; }

    [Header("UI 组件")]           // Inspector分组
    public GameObject panelObject;      // 提示面板的根物体
    public TextMeshProUGUI contentText; // 显示提示内容的文本框

    [Header("设置")]
    public bool pauseGame = false;       // 是否在打开面板时暂停游戏

    private void Awake()
    {
        // 标准单例实现：如果不存在则设置为当前实例，否则销毁当前对象
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HidePanel(); // 初始化时隐藏面板
    }

    // =========================================================
    // 【核心优化】新增 Update 方法
    // 作用：实时保护鼠标状态。只要面板开着，就强制鼠标显示。
    // 这能完美解决“关闭设置面板后，提示面板还在，但鼠标没了”的Bug。
    // =========================================================
    private void Update()
    {
        // 如果面板存在且处于激活状态
        if (panelObject != null && panelObject.activeSelf)
        {
            // 如果发现鼠标被别人偷偷藏起来了，或者锁住了（例如设置面板关闭后错误地锁了鼠标）
            if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
            {
                // 立刻强制恢复显示！
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    // === 打开面板 ===
    /// <summary>
    /// 显示提示面板，设置文本内容，并根据设置决定是否暂停游戏。
    /// 同时强制显示鼠标。
    /// </summary>
    /// <param name="text">要显示的提示文本</param>
    public void ShowPanel(string text)
    {
        Debug.Log($"[TutorPanel] ShowPanel 被调用，文本内容：{text}，当前面板激活状态：{panelObject?.activeSelf}");
        if (panelObject == null)
        {
            Debug.LogError("[TutorPanel] panelObject 为 null！");
            return;
        }
        // 检查父物体激活状态
        Transform parent = panelObject.transform.parent;
        if (parent != null)
        {
            Debug.Log($"[TutorPanel] 父物体 {parent.name} 激活状态：{parent.gameObject.activeSelf}");
        }
        else
        {
            Debug.Log("[TutorPanel] panelObject 没有父物体");
        }

        contentText.text = text;
        panelObject.SetActive(true);
        Debug.Log($"[TutorPanel] SetActive(true) 后，面板激活状态：{panelObject.activeSelf}");
        // ... 其余代码
    }

    // === 关闭面板 ===
    /// <summary>
    /// 隐藏提示面板，并根据设置恢复游戏时间。
    /// 关闭后，如果面板确实已隐藏，则恢复鼠标锁定（适用于第一人称场景）。
    /// </summary>
    public void HidePanel()
    {
        panelObject.SetActive(false);      // 隐藏面板
        PlayBtnSound();                     // 播放按钮音效（关闭音效）

        if (pauseGame)
        {
            Time.timeScale = 1f;            // 恢复游戏时间
        }

        // 只有当面板真的关闭了，才锁住鼠标（适合第一人称场景）
        if (!panelObject.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // === 内部辅助方法 ===
    /// <summary>
    /// 播放面板打开音效（使用PanelOpenSound）
    /// </summary>
    private void PlayPanelSound()
    {
        // 检查AudioManager、BtnSource、GameData和PanelOpenSound是否存在
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.ButtonClickSound)
        {
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
        }
    }

    /// <summary>
    /// 播放按钮点击音效（关闭面板时使用ButtonClickSound）
    /// </summary>
    private void PlayBtnSound()
    {
        // 注意：这里使用了ButtonClickSound，而不是PanelOpenSound
        if (AudioManager.Instance && AudioManager.Instance.BtnSource &&
            GameData.Instance && GameData.Instance.PanelOpenSound)
        {
            AudioManager.Instance.BtnSource.PlayOneShot(GameData.Instance.ButtonClickSound);
        }
    }
}