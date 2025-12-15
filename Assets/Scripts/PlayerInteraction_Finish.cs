using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerInteraction_Finish : MonoBehaviour
{
    [Header("射线检测的最大距离")]
    public float interactionDistance = 10.0f;

    // 目标物体的标签
    private const string targetTag = "Picture";

    // 关键：我们要忽略的层级名字
    private const string ignoreLayerName = "Player";

    // 最终计算出的遮罩
    private int finalLayerMask;

    private void Start()
    {
        // 找到 "Player" 层的索引
        int playerLayerIndex = LayerMask.NameToLayer(ignoreLayerName);

        if (playerLayerIndex != -1)
        {
            // 2. 核心数学公式：
            // 1 << index  -> 只开启 Player 层
            // ~ (...)     -> 取反，变成“开启除了Player以外的所有层”
            finalLayerMask = ~(1 << playerLayerIndex);

            Debug.Log($"[初始化成功] 已设置射线遮罩，将忽略层级: {ignoreLayerName}");
        }
        else
        {
            // 如果没找到这个层，就检测所有东西（会出问题，但也比报错好）
            Debug.LogError($"[初始化警告] 找不到名为 '{ignoreLayerName}' 的Layer！请检查设置。");
            finalLayerMask = ~0;
        }
    }

    private void Update()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        // 调试绘制：黄色线 (只有在 Scene 视图能看到)
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.yellow);

        // 3. 这里的关键参数：finalLayerMask
        // 它告诉 Unity：“请检测所有物体，唯独跳过 Player 层”
        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            if (hit.collider.gameObject.CompareTag(targetTag))
            {
                // 只有这里检测到 Picture 才输出，避免刷屏
                Debug.Log($"【对准成功】发现画框的{hit.collider.gameObject.name}");
                //交互函数
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("--- 交互触发 ---");
                }
            }
        }
    }
}