// 文件：RouteLines.cs
// 模块：导航 / 路径指示器
// 说明：该脚本用于在地面上绘制一条可跟随的路径线，通常用于引导玩家前进。
//      它通过LineRenderer组件显示路径，并随着玩家移动动态更新：当玩家接近或超过某个路径点时，
//      该点会被移除，使得路径线缩短，实现“指引”效果。路径线材质支持滚动动画，营造箭头流动感。
// 特性：依赖LineRenderer组件，使用世界空间坐标，通过算法判断路径点是否被经过，
//      支持实时更新路径点列表，材质UV滚动实现动画。

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteLines : MonoBehaviour
{
    [Header("=== 核心设置 ===")]
    public Transform player;                // 玩家Transform，用于跟踪玩家位置
    public float groundOffset = 0.1f;       // 线条离地高度，防止与地面重叠

    [Header("=== 智能导航设置 ===")]
    public float reachThreshold = 3.0f;      // 判定玩家到达路径点的距离阈值（增大可提高容错）

    [Header("=== 箭头动画 ===")]
    public float scrollSpeed = 2.0f;         // 材质纹理滚动速度，用于制作箭头流动动画

    private LineRenderer lineRenderer;       // 路径线的渲染组件
    private Material lineMaterial;           // 路径线的材质（用于动画）

    
    // 存储尚未经过的路径点列表（世界坐标）
    private List<Vector3> targetWaypoints = new List<Vector3>();

    void Start()
    {
        // 获取组件
        lineRenderer = GetComponent<LineRenderer>();
        lineMaterial = lineRenderer.material;

        // 强制修正LineRenderer的关键设置，确保路径线正确显示
        lineRenderer.useWorldSpace = true;          // 使用世界空间坐标
        lineRenderer.textureMode = LineTextureMode.Tile; // 纹理平铺模式，使箭头连续
        lineRenderer.alignment = LineAlignment.TransformZ; // 线条对齐方式，使箭头朝上

        // 1. 获取初始路径点
        Vector3[] initPoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(initPoints);      // 将当前LineRenderer的点复制到数组中

        // 从 Index 1 开始存（因为 Index 0 是玩家脚下，不需要存）
        // 设计上，LineRenderer的第一个点通常是玩家当前所在位置，后续点为路径点
        for (int i = 1; i < initPoints.Length; i++)
        {
            targetWaypoints.Add(initPoints[i]);    // 存储真正的路径点
        }
    }

    void Update()
    {
        if (player == null) return;

        // 获取玩家平面坐标（忽略高度差，只考虑XZ平面）
        Vector3 playerFlat = new Vector3(player.position.x, 0, player.position.z);

        // 2. 【核心升级】贪婪消除逻辑
        // 使用 while 循环，一次可能消除多个过期的点，确保路径实时更新
        while (targetWaypoints.Count > 0)
        {
            // 当前第一个目标点（忽略高度）
            Vector3 currentPointFlat = new Vector3(targetWaypoints[0].x, 0, targetWaypoints[0].z);
            float distToCurrent = Vector3.Distance(playerFlat, currentPointFlat);

            // 条件A：距离非常近（玩家踩到了） -> 消除该点
            if (distToCurrent < reachThreshold)
            {
                targetWaypoints.RemoveAt(0);
                continue; // 继续检查下一个点（可能玩家同时踩到了多个点）
            }

            // 条件B：离“下一个点”比“当前点”更近 -> 说明玩家切角路过了当前点，也应消除
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

        // 3. 重新绘制 LineRenderer，更新路径线
        UpdateLineRenderer();

        // 4. 箭头动画：滚动材质的纹理偏移量，产生流动效果
        if (lineMaterial != null)
        {
            Vector2 textureOffset = lineMaterial.mainTextureOffset;
            textureOffset.x -= Time.deltaTime * scrollSpeed;   // 向左滚动
            lineMaterial.mainTextureOffset = textureOffset;
        }
    }

    /// <summary>
    /// 根据剩余的路径点重新绘制LineRenderer。
    /// 第一个点始终为玩家当前位置（加地面偏移），后续点为剩余路径点。
    /// </summary>
    void UpdateLineRenderer()
    {
        // 点的总数 = 1个玩家 + 剩下的路点
        lineRenderer.positionCount = 1 + targetWaypoints.Count;

        // 设置第 0 个点：玩家脚下，并加上地面偏移以避免穿模
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