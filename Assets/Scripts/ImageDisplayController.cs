// 文件：ImageDisplayController.cs
// 模块：展示场景 / 图文展示
// 说明：该脚本负责图文展示场景（ImageContent）的UI控制和音频管理。
//      它从GameData.CurrentImage获取展品数据，显示标题、描述和图片，
//      并自动播放解说音频（延迟3秒）。同时监听设置面板状态，在面板打开时暂停音频，
//      面板关闭时恢复播放，并确保鼠标始终可见。
// 特性：使用协程实现延迟播放，通过Cursor控制鼠标状态，依赖SettingPanel单例判断面板状态。

using UnityEngine;
using UnityEngine.UI;      // 使用UI.Image
using TMPro;               // 使用TextMeshPro文本
using System.Collections;  // 使用协程

public class ImageDisplayController : MonoBehaviour
{
    // UI组件绑定，需在Inspector中拖拽赋值
    public TMP_Text titleText;           // 显示展品标题的文本框
    public Image contentImage;            // 显示展品图片的Image组件
    public TMP_Text descriptionText;      // 显示展品描述的文本框
    public Image backgroundRenderer;      // 背景图渲染（随机背景）

    // 私有变量：标记解说音频是否因设置面板打开而被暂停
    private bool isPaused = false;

    void Start()
    {
        // 强制解锁并显示鼠标（因为图文展示场景需要鼠标操作返回等）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 如果GameData实例存在且背景渲染器不为空，则设置随机背景图
        if (GameData.Instance && backgroundRenderer)
            backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();

        // 检查是否有当前图文展品数据
        if (GameData.CurrentImage != null)
        {
            var data = GameData.CurrentImage;   // 获取数据包
            // 设置标题、图片、描述（先检查组件不为空）
            if (titleText) titleText.text = data.Title;
            if (contentImage) contentImage.sprite = data.ImageContent;
            if (descriptionText) descriptionText.text = data.Description;

            // 路由解说音频到DesSource（解说音频源）
            // 条件：自动播放开启、解说音频存在、AudioManager存在且其DesSource存在
            if (data.AutoPlayVoice && data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;     // 将音频剪辑赋值给音频源

                // 【核心修改】启动延迟播放协程，延迟3秒后播放
                StartCoroutine(DelayPlayVoice(des));
            }
        }
    }

    // 【修改】将延迟时间改为 3.0 秒
    /// <summary>
    /// 延迟播放语音的协程，等待指定秒数后播放音频。
    /// 播放前会检查是否被暂停，防止在设置面板打开时突然播放。
    /// </summary>
    /// <param name="source">要播放的AudioSource</param>
    IEnumerator DelayPlayVoice(AudioSource source)
    {
        yield return new WaitForSeconds(3.0f);   // 等待3秒

        // 播放前检查：没有被暂停（即设置面板未打开）、音频源存在、剪辑存在
        if (!isPaused && source && source.clip)
        {
            source.Play();   // 播放解说音频
        }
    }

    void Update()
    {
        // 监听设置面板状态，控制解说音频的暂停与恢复
        // 条件：SettingPanel单例存在、AudioManager存在且DesSource存在
        if (SettingPanel.Instance && AudioManager.Instance && AudioManager.Instance.DesSource)
        {
            var des = AudioManager.Instance.DesSource;
            bool panelOpen = SettingPanel.Instance.isPanelActive;   // 获取设置面板是否打开

            // 如果面板打开且当前没有标记为暂停（即尚未暂停）
            if (panelOpen && !isPaused)
            {
                // 如果音频正在播放，则暂停
                if (des.isPlaying) des.Pause();
                isPaused = true;   // 标记为暂停状态
            }
            // 如果面板关闭且当前标记为暂停（即之前因面板打开而暂停）
            else if (!panelOpen && isPaused)
            {
                // 如果音频剪辑存在，则恢复播放（从暂停处继续）
                if (des.clip != null) des.UnPause();
                isPaused = false;   // 取消暂停标记

                // 【双重保险】防止鼠标丢失（有时面板关闭后鼠标仍被锁定）
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }
}