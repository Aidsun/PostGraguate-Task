using UnityEngine;
using UnityEngine.Video;
using TMPro;

public class PanoramaDisplayController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Material skyboxMat;
    public TMP_Text titleText;

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
                // 创建RT
                rt = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = rt;
                if (skyboxMat)
                {
                    skyboxMat.SetTexture("_MainTex", rt);
                    RenderSettings.skybox = skyboxMat;
                }
                videoPlayer.clip = data.PanoramaContent;

                // 路由声音到 VidAudio
                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource);
                }
                videoPlayer.Play();
            }

            // 播放解说 DesAudio
            if (data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;
                if (data.AutoPlayVoice) des.Play();
            }
        }
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }

    void Update()
    {
        if (SettingPanel.Instance)
        {
            bool panelOpen = SettingPanel.Instance.isPanelActive;
            if (panelOpen && !isPaused)
            {
                if (videoPlayer.isPlaying) videoPlayer.Pause();
                if (AudioManager.Instance.DesSource.isPlaying) AudioManager.Instance.DesSource.Pause();
                isPaused = true;
            }
            else if (!panelOpen && isPaused)
            {
                videoPlayer.Play();
                if (AudioManager.Instance.DesSource.clip != null) AudioManager.Instance.DesSource.UnPause();
                isPaused = false;
            }
        }
    }
}