using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

public class VideoExhibition : MonoBehaviour
{
    [Header("视频展品信息")]

    [Tooltip("视频标题")]
    public string VideoTitle;
    [Tooltip("视频文件")]
    public VideoClip VideoFile;
    [Tooltip("视频封面")]
    public Sprite VideoCover;
    [Tooltip("视频描述")]
    [TextArea(5,10)] public string VideoDescriptionText;

    [Header("组件设置")]

    [Tooltip("高亮组件")]
    public Renderer outlineRenderer;
    [Tooltip("标题显示组件")]
    public TMP_Text ShowTitle;


    [Header("跳转设置")]

    [Tooltip("跳转目标场景名称")]
    public string targetSceneName = "VideoContent";

    private void Start()
    {
        //初始化标题显示组件的文本
        if(ShowTitle != null )
        {
            ShowTitle.text = "《" + VideoTitle + "》";
        }
        else
        {
            Debug.LogWarning("展示标题ShowTitle组件未绑定，无法显示标题。");
        }
        //初始化检测高亮组件
        if(outlineRenderer == null)
        {
            Debug.LogWarning("高亮组件outlineRenderer未绑定，无法进行高亮显示。");
        }
    }

    //设置高亮函数
    public void SetHighlight(bool isActive)
    {
        outlineRenderer.material.color = isActive ? Color.yellow : Color.white;
    }

}
