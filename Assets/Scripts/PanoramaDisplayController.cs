using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

public class PanoramaDisplayController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Material skyboxMat;
    public TMP_Text titleText;
    public AudioSource voiceSource;
    // [É¾³ý] public Button exitButton;

    private RenderTexture rt;
    private bool isPaused = false;

    void Start()
    {
        if (GameData.CurrentPanorama != null)
        {
            var data = GameData.CurrentPanorama;
            if (titleText) titleText.text = data.Title;

            if (videoPlayer)
            {
                rt = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = rt;
                if (skyboxMat) { skyboxMat.SetTexture("_MainTex", rt); RenderSettings.skybox = skyboxMat; }
                videoPlayer.clip = data.PanoramaContent;
                videoPlayer.Play();
            }
            if (data.AutoPlayVoice && data.VoiceClip != null && voiceSource) { voiceSource.clip = data.VoiceClip; voiceSource.Play(); }
        }
        // [É¾³ý] exitButton °ó¶¨
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }

    void Update()
    {
        if (GameData.Instance)
        {
            if (videoPlayer) videoPlayer.SetDirectAudioVolume(0, GameData.Instance.VideoVolume);
            if (voiceSource) voiceSource.volume = GameData.Instance.VoiceVolume;
        }

        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;
            if (panelOpen && !isPaused) { if (videoPlayer.isPlaying) videoPlayer.Pause(); isPaused = true; }
            else if (!panelOpen && isPaused) { videoPlayer.Play(); isPaused = false; }
        }
    }
}