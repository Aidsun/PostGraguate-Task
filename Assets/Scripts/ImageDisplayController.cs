using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ImageDisplayController : MonoBehaviour
{
    [Header("组件绑定")]
    public TMP_Text titleText;
    public Image contentImage;
    public TMP_Text descriptionText;
    public Image backgroundRenderer;
    public AudioSource voiceSource;
    // [删除] public Button exitButton;  <- 不再需要

    private bool isPaused = false;

    void Start()
    {
        if (GameData.Instance)
        {
            if (backgroundRenderer) backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();
        }

        if (GameData.CurrentImage != null)
        {
            var data = GameData.CurrentImage;
            if (titleText) titleText.text = data.Title;
            if (contentImage) contentImage.sprite = data.ImageContent;
            if (descriptionText) descriptionText.text = data.Description;

            if (data.AutoPlayVoice && data.VoiceClip != null && voiceSource)
            {
                voiceSource.clip = data.VoiceClip;
                StartCoroutine(DelayPlayVoice());
            }
        }
        // [删除] exitButton 绑定监听逻辑
    }

    IEnumerator DelayPlayVoice()
    {
        yield return new WaitForSeconds(3.0f);
        if (!isPaused && voiceSource && voiceSource.clip) voiceSource.Play();
    }

    void Update()
    {
        if (GameData.Instance && voiceSource) voiceSource.volume = GameData.Instance.VoiceVolume;

        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;
            if (panelOpen && !isPaused) { if (voiceSource.isPlaying) voiceSource.Pause(); isPaused = true; }
            else if (!panelOpen && isPaused) { voiceSource.UnPause(); isPaused = false; }
        }
    }
}