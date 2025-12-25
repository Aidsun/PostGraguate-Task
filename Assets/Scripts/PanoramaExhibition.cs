using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class PanoramaExhibition : MonoBehaviour
{
    [Header("全景展品信息")]
    public string PanoramaTitle;
    public VideoClip PanoramaFile;
    public Sprite PanoramaCover;
    [TextArea(5, 10)] public string PanoramaDescriptionText;

    [Header("解说设置")]
    [Tooltip("是否启用语音解说？")]
    public bool enableVoiceover = true; // 开关
    [Tooltip("全景解说音频")]
    public AudioClip DescriptionAudio;

    [Header("组件设置")]
    public Renderer ContentCover;
    public Renderer outlineRenderer;
    public TMP_Text ShowTitle; // 【修正】修正了之前的拼写错误 (ShowTitile)

    [Header("跳转场景")]
    public string targetSceneName = "PanoramaContent";

    private void Start()
    {
        if (ShowTitle != null) ShowTitle.text = "《" + PanoramaTitle + "》";

        if (ContentCover != null && PanoramaCover != null)
        {
            ContentCover.material.shader = Shader.Find("Unlit/Texture");
            ContentCover.material.mainTexture = PanoramaCover.texture;
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (outlineRenderer != null)
            outlineRenderer.material.color = isActive ? Color.blue : Color.white;
    }

    public void StartDisplay()
    {
        // 1. 打包数据
        GameDate.PanoramaDate dataPackage = new GameDate.PanoramaDate();
        dataPackage.Title = this.PanoramaTitle;

        // 【关键修正】这里必须是大写的 PanoramaFile，对应 GameDate 中的定义
        dataPackage.PanoramaFile = this.PanoramaFile;

        // 音频逻辑
        dataPackage.DescriptionAudio = enableVoiceover ? this.DescriptionAudio : null;

        // 2. 发送数据
        GameDate.CurrentPanoramaDate = dataPackage;

        // 3. 保存当前位置
        SavePlayerPosition();

        // 4. 跳转场景
        if (System.Type.GetType("SceneLoding") != null)
            SceneLoding.LoadLevel(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }

    private void SavePlayerPosition()
    {
        SwitchViews switchScripts = FindObjectOfType<SwitchViews>();
        if (switchScripts != null)
        {
            Transform activePlayer = switchScripts.GetActivePlayerTransform();

            GameDate.LastPlayerPosition = activePlayer.position;
            GameDate.LastPlayerRotation = activePlayer.rotation;

            // 记录进入时的视角状态
            GameDate.WasFirstPerson = switchScripts.IsInFirstPerson();

            // 【注意】这里不要设置 ShouldRestorePosition = true
            // 那个标志位应该在“从展品返回”时（DisplayController）设置

            Debug.Log($"全景展品：已保存玩家位置 {activePlayer.position}");
        }
    }
}