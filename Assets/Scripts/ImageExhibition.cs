using UnityEngine;
using UnityEngine.SceneManagement;

public class ImageExhibition : MonoBehaviour
{
    [Header("展品信息")]
    public string ImageTitle;
    [TextArea(5, 10)] public string ImageDescriptionText;
    public Sprite ImageSprite;
    public AudioClip artAudioClip;

    [Header("高亮设置")]
    public Renderer outlineRenderer;

    [Header("跳转目标场景")]
    public string targetSceneName = "ImageContent";

    public void SetHighlight(bool isActive)
    {
        if (outlineRenderer != null)
            outlineRenderer.material.color = isActive ? Color.yellow : Color.white;
    }

    public void StartDisplay()
    {
        // 1. 打包展品数据
        GameDate.ImageDate dataPackage = new GameDate.ImageDate();
        dataPackage.Title = this.ImageTitle;
        dataPackage.DescriptionText = this.ImageDescriptionText;
        dataPackage.ImageShow = this.ImageSprite;
        dataPackage.DescriptionAudio = this.artAudioClip;
        GameDate.CurrentImageData = dataPackage;

        // ------------------ 【关键修改：完整保存逻辑】 ------------------
        // 2. 找到玩家并保存位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 保存位置和旋转
            GameDate.LastPlayerPosition = player.transform.position;
            GameDate.LastPlayerRotation = player.transform.rotation;
            GameDate.ShouldRestorePosition = true; // 标记开关

            // 【关键】取消注释：保存当前的人称视角
            SwitchViews switchScript = player.GetComponent<SwitchViews>();
            if (switchScript != null)
            {
                GameDate.WasFirstPerson = switchScript.IsInFirstPerson();
            }

            Debug.Log($"[保存成功] 位置:{GameDate.LastPlayerPosition}, 视角:{(GameDate.WasFirstPerson ? "第一" : "第三")}");
        }
        else
        {
            Debug.LogError("【严重警告】未找到Tag为Player的物体！位置无法保存！请检查玩家Tag设置。");
        }
        // ------------------ 【修改结束】 ------------------

        // 3. 跳转场景
        SceneManager.LoadScene(targetSceneName);
    }
}