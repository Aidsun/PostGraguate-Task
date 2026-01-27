using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteLines : MonoBehaviour
{
    [Header("=== 核心设置 ===")]
    public Transform player;
    public float groundOffset = 0.1f; // 线条离地高度

    [Header("=== 智能导航设置 ===")]
    public float reachThreshold = 3.0f; // 增大判定范围

    [Header("=== 箭头动画 ===")]
    public float scrollSpeed = 2.0f;

    private LineRenderer lineRenderer;
    private Material lineMaterial;

    // 存储路径点列表
    private List<Vector3> targetWaypoints = new List<Vector3>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineMaterial = lineRenderer.material;

        // 强制修正设置
        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.TransformZ;

        // 1. 获取初始路径点
        Vector3[] initPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(initPoints);

        // 从 Index 1 开始存（因为 Index 0 是玩家脚下，不需要存）
        for (int i = 1; i < initPoints.Length; i++)
        {
            targetWaypoints.Add(initPoints[i]);
        }
    }

    void Update()
    {
        if (player == null) return;

        // 获取玩家平面坐标（忽略高度差）
        Vector3 playerFlat = new Vector3(player.position.x, 0, player.position.z);

        // 2. 【核心升级】贪婪消除逻辑
        // 使用 while 循环，一次可能消除多个过期的点
        while (targetWaypoints.Count > 0)
        {
            Vector3 currentPointFlat = new Vector3(targetWaypoints[0].x, 0, targetWaypoints[0].z);
            float distToCurrent = Vector3.Distance(playerFlat, currentPointFlat);

            // 条件A：距离非常近（踩到了） -> 消除
            if (distToCurrent < reachThreshold)
            {
                targetWaypoints.RemoveAt(0);
                continue; // 继续检查下一个点
            }

            // 条件B：离“下一个点”比“当前点”更近 -> 说明切角路过了 -> 消除当前点
            if (targetWaypoints.Count > 1)
            {
                Vector3 nextPointFlat = new Vector3(targetWaypoints[1].x, 0, targetWaypoints[1].z);
                float distToNext = Vector3.Distance(playerFlat, nextPointFlat);

                // 如果离下一个点更近，说明当前点已经是“过去式”了
                if (distToNext < distToCurrent)
                {
                    targetWaypoints.RemoveAt(0);
                    continue; // 继续检查
                }
            }

            // 如果上面两个条件都不满足，说明前面的路点还没走完，跳出循环
            break;
        }

        // 3. 重新绘制 LineRenderer
        UpdateLineRenderer();

        // 4. 动画播放
        if (lineMaterial != null)
        {
            Vector2 textureOffset = lineMaterial.mainTextureOffset;
            textureOffset.x -= Time.deltaTime * scrollSpeed;
            lineMaterial.mainTextureOffset = textureOffset;
        }
    }

    void UpdateLineRenderer()
    {
        // 点的总数 = 1个玩家 + 剩下的路点
        lineRenderer.positionCount = 1 + targetWaypoints.Count;

        // 设置第 0 个点：玩家脚下
        Vector3 startPos = player.position;
        startPos.y += groundOffset;
        lineRenderer.SetPosition(0, startPos);

        // 设置剩下的点
        for (int i = 0; i < targetWaypoints.Count; i++)
        {
            lineRenderer.SetPosition(i + 1, targetWaypoints[i]);
        }
    }
}
