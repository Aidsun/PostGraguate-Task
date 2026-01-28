using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class PanoramaDisplayController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Material skyboxMat;
    public TMP_Text titleText;

    private RenderTexture rt;
    private bool isPaused = false;

    void Start()
    {
        // 1. 强制暂停 BGM
        if (AudioManager.Instance && AudioManager.Instance.BgmSource)
        {
            AudioManager.Instance.BgmSource.Pause();
        }

        if (GameData.CurrentPanorama != null)
        {
            var data = GameData.CurrentPanorama;
            if (titleText) titleText.text = data.Title;

            if (videoPlayer)
            {
                rt = new RenderTexture(4096, 2048, 0);
                videoPlayer.targetTexture = rt;
                if (skyboxMat)
                {
                    skyboxMat.SetTexture("_MainTex", rt);
                    RenderSettings.skybox = skyboxMat;
                }
                videoPlayer.clip = data.PanoramaContent;

                // 【核心修复】动态连接音频
                if (AudioManager.Instance && AudioManager.Instance.VidSource)
                {
                    AudioManager.Instance.VidSource.Stop();
                    AudioManager.Instance.VidSource.clip = null;
                    AudioManager.Instance.VidSource.volume = 1.0f;
                    AudioManager.Instance.VidSource.mute = false;

                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.EnableAudioTrack(0, true);
                    videoPlayer.SetTargetAudioSource(0, AudioManager.Instance.VidSource);
                }

                videoPlayer.Play();
            }

            // 播放解说
            if (data.VoiceClip != null && AudioManager.Instance && AudioManager.Instance.DesSource)
            {
                var des = AudioManager.Instance.DesSource;
                des.clip = data.VoiceClip;
                if (data.AutoPlayVoice) StartCoroutine(PlayVoiceWithDelay(des, 3.0f));
            }
        }
    }

    IEnumerator PlayVoiceWithDelay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isPaused && source && source.clip != null) source.Play();
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