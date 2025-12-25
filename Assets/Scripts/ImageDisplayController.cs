using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ImageDisplayController : MonoBehaviour
{
    [Header("UI 组件绑定")]
    public TMP_Text imageTitle;
    public Image imageShow;
    public TMP_Text imageDescription;

    [Header("图片解说")]
    public AudioSource imageAudio;

    // 当前解说音量（从设置面板获取）
    private float currentDescriptionVolume = 1.0f;

    void Awake()
    {
        // 注册到设置面板，接收配置更新
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        // 注销设置应用方法
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void Start()
    {
        // 应用当前设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }

        // 1. 获取数据
        var data = GameDate.CurrentImageData;

        if (data != null)
        {
            if (imageTitle) imageTitle.text = data.Title;
            if (imageShow && data.ImageFile) imageShow.sprite = data.ImageFile;
            if (imageDescription) imageDescription.text = data.DescriptionText;

            if (imageAudio && data.DescriptionAudio)
            {
                imageAudio.clip = data.DescriptionAudio;
                imageAudio.volume = currentDescriptionVolume; // 应用音量设置
                imageAudio.Play();
            }
        }
        else
        {
            Debug.LogError("图片展品：未能获取到有效的图片数据！");
        }

        // 3. 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        // 应用解说音量
        currentDescriptionVolume = settings.descriptionVolume;

        // 更新已初始化的音频音量
        if (imageAudio != null)
        {
            imageAudio.volume = currentDescriptionVolume;
        }

        Debug.Log($"ImageDisplayController: 应用设置 - 解说音量: {currentDescriptionVolume}");
    }

    // 可选：添加控制方法
    void Update()
    {
        // 如果设置面板打开，暂停解说音频
        if (SettingPanel.Instance != null && SettingPanel.Instance.isPanelActive && imageAudio != null && imageAudio.isPlaying)
        {
            imageAudio.Pause();
        }

        // 如果设置面板关闭且音频被暂停，恢复播放
        if (SettingPanel.Instance != null && !SettingPanel.Instance.isPanelActive && imageAudio != null && !imageAudio.isPlaying && imageAudio.clip != null)
        {
            // 确保音频源有clip并且应该播放
            if (imageAudio.clip != null)
            {
                imageAudio.UnPause();
            }
        }

        // 支持空格键重新播放解说
        if (Input.GetKeyDown(KeyCode.Space) && imageAudio != null && imageAudio.clip != null)
        {
            if (imageAudio.isPlaying)
            {
                imageAudio.Stop();
                imageAudio.Play();
            }
            else
            {
                imageAudio.Play();
            }
        }
    }
}