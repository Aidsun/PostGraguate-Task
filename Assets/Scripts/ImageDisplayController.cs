// 文件：ImageDisplayController.cs
// 模块：展示场景 / 图文展示
// 说明：该脚本负责图文展示场景（ImageContent）的UI控制和音频管理。
//      它从GameData.CurrentImage获取展品数据，显示标题、描述和图片，
//      并自动播放解说音频（延迟3秒）。同时监听设置面板状态，在面板打开时暂停音频，
//      面板关闭时恢复播放，并确保鼠标始终可见。
// 特性：使用协程实现延迟播放，通过Cursor控制鼠标状态，依赖SettingPanel单例判断面板状态。

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ImageDisplayController : MonoBehaviour
{
    public TMP_Text titleText;
    public Image contentImage;
    public TMP_Text descriptionText;
    public Image backgroundRenderer;

    private bool isPaused = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameData.Instance && backgroundRenderer)
            backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();

        if (GameData.CurrentImage != null)
        {
            var data = GameData.CurrentImage;
            if (titleText) titleText.text = data.Title;
            if (contentImage) contentImage.sprite = data.ImageContent;
            if (descriptionText) descriptionText.text = data.Description;

            if (data.AutoPlayVoice && data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;
                StartCoroutine(DelayPlayVoice(des));
            }
        }
    }

    IEnumerator DelayPlayVoice(AudioSource source)
    {
        yield return new WaitForSeconds(3.0f);
        if (!isPaused && source && source.clip)
        {
            source.Play();
        }
    }

    void Update()
    {
        if (SettingPanel.Instance && AudioManager.Instance && AudioManager.Instance.DesSource)
        {
            var des = AudioManager.Instance.DesSource;
            bool panelOpen = SettingPanel.Instance.isPanelActive;

            if (panelOpen && !isPaused)
            {
                if (des.isPlaying) des.Pause();
                isPaused = true;
            }
            else if (!panelOpen && isPaused)
            {
                if (des.clip != null) des.UnPause();
                isPaused = false;

                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }

    // 当场景被销毁时，停止解说音频，防止返回主馆后继续播放
    void OnDestroy()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.DesSource != null)
        {
            AudioManager.Instance.DesSource.Stop();
            AudioManager.Instance.DesSource.clip = null;
        }
    }
}