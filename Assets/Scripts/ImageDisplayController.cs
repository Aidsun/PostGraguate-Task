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
    public Button exitButton;

    [Header("音频组件")]
    public AudioSource imageAudio;

    [Header("退出跳转场景")]
    [Tooltip("点击返回按钮后，要跳转回哪个场景的名字？(例如 Museum_Main)")]
    public string returnSceneName = "Museum_Main"; // 默认值，您可以在面板修改

    void Start()
    {
        // 1. 取数据
        var data = GameDate.CurrentImageData;

        if (data != null)
        {
            if (imageTitle) imageTitle.text = data.Title;
            if (imageShow && data.ImageShow) imageShow.sprite = data.ImageShow;
            if (imageDescription) imageDescription.text = data.DescriptionText;

            if (imageAudio && data.DescriptionAudio)
            {
                imageAudio.clip = data.DescriptionAudio;
                imageAudio.Play();
            }
        }
        else
        {
            Debug.LogError("图片展示场景没有读到画框数据！");
        }

        // 2. 绑定返回按钮
        if (exitButton)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }

        // 3. 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnExitButtonClicked()
    {
        if (imageAudio) imageAudio.Stop();

        // 使用配置好的变量进行跳转
        if (!string.IsNullOrEmpty(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
        else
        {
            Debug.LogError("返回场景名字为空！请在 Inspector 面板检查 Return Scene Name 设置");
        }
    }
}