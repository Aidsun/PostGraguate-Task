using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorCube : MonoBehaviour
{
    [Header("在这里写提示内容")]
    [TextArea(3, 10)] // 让输入框变大，方便写多行文字
    public string myTipContent = "欢迎来到浏览馆！";

    [Header("设置")]
    public bool oneTimeOnly = true; // 是否是一次性的？（触发过一次就销毁）

    private void OnTriggerEnter(Collider other)
    {
        // 检测进入的是否是玩家 (记得给玩家物体设置 Tag 为 "Player")
        if (other.CompareTag("Player"))
        {
            // 呼叫 UI 管理器显示内容
            TutorPanel.Instance.ShowPanel(myTipContent);

            // 如果是一次性的，触发完就关掉自己，防止反复弹窗烦人
            if (oneTimeOnly)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
