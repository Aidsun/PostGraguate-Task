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
        if (GameData.Instance && backgroundRenderer)
            backgroundRenderer.sprite = GameData.Instance.GetRandomContentBG();

        if (GameData.CurrentImage != null)
        {
            var data = GameData.CurrentImage;
            if (titleText) titleText.text = data.Title;
            if (contentImage) contentImage.sprite = data.ImageContent;
            if (descriptionText) descriptionText.text = data.Description;

            // 路由解说 DesAudio
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
        yield return new WaitForSeconds(1.0f);
        if (!isPaused && source && source.clip) source.Play();
    }

    void Update()
    {
        if (SettingPanel.Instance && AudioManager.Instance && AudioManager.Instance.DesSource)
        {
            var des = AudioManager.Instance.DesSource;
            bool panelOpen = SettingPanel.Instance.isPanelActive;

            if (panelOpen && !isPaused) { if (des.isPlaying) des.Pause(); isPaused = true; }
            else if (!panelOpen && isPaused) { des.UnPause(); isPaused = false; }
        }
    }
}