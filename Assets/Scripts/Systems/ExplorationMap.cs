using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class ExplorationMap : MonoBehaviour
{
    public static ExplorationMap Instance;

    public float gridSize = 1.0f;
    private Dictionary<Vector2Int, bool> visitedGrids = new Dictionary<Vector2Int, bool>();

    void Awake()
    {
        Instance = this;
    }

    public void MarkAsVisited(Vector3 worldPos, float radius)
    {
        Vector2Int center = WorldToGrid(worldPos);
        int r = Mathf.CeilToInt(radius / gridSize);

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                Vector2Int cell = new Vector2Int(center.x + x, center.y + y);
                if (Vector3.Distance(worldPos, GridToWorld(cell)) <= radius)
                {
                    if (!visitedGrids.ContainsKey(cell)) visitedGrids.Add(cell, true);
                }
            }
        }
    }

    public bool IsVisited(Vector3 worldPos)
    {
        return visitedGrids.ContainsKey(WorldToGrid(worldPos));
    }

    // --- 核心增强：32点高精度全方位扫描 ---
    // --- 修改后的核心函数 ---
    // --- 修改后的核心函数 ---
    // --- ExplorationMap.cs 修正部分 ---

    public Vector3 GetUnexploredPoint(Vector3 currentPos, float detectionRadius)
    {
        Vector3 bestDirection = Vector3.zero;
        float maxUnexploredCount = -1;
        float visionRadius = 12f;

        int sampleCount = 32;
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * (Mathf.PI * 2 / sampleCount);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // --- 修正点：检查目标点是否在地图（NavMesh）内 ---
            Vector3 checkPoint = currentPos + direction * detectionRadius;
            NavMeshHit hit;
            // 只有当该方向的点在导航网格附近时才进行评分
            if (!NavMesh.SamplePosition(checkPoint, out hit, 3.0f, NavMesh.AllAreas))
            {
                continue; // 如果这个方向是地图外或障碍物，跳过该角度
            }

            int density = CountUnexploredInRing(currentPos, direction, visionRadius, detectionRadius);

            float score = density + Random.Range(0f, 0.5f);
            if (score > maxUnexploredCount)
            {
                maxUnexploredCount = score;
                bestDirection = direction;
            }
        }

        // 如果环带内全是已探索区域，执行受限的随机跳出
        if (maxUnexploredCount <= 0.5f)
        {
            // 随机找一个点，但必须强制约束在 NavMesh 内
            Vector2 rnd = Random.insideUnitCircle.normalized * 15f;
            Vector3 target = currentPos + new Vector3(rnd.x, 0, rnd.y);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 20.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return currentPos;
        }

        return currentPos + bestDirection * detectionRadius;
    }

    // 新增：沿射线在环带区间采样格子[cite: 3]
    private int CountUnexploredInRing(Vector3 origin, Vector3 dir, float minR, float maxR)
    {
        int count = 0;
        // 按照网格大小 gridSize 步进，确保覆盖到区间内的每个潜在格子[cite: 3]
        for (float d = minR; d <= maxR; d += gridSize)
        {
            Vector3 samplePos = origin + dir * d;
            if (!IsVisited(samplePos)) count++;
        }
        return count;
    }

    private int CountUnexploredNearby(Vector3 pos, int range)
    {
        int count = 0;
        Vector2Int centerGrid = WorldToGrid(pos);
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int checkGrid = new Vector2Int(centerGrid.x + x, centerGrid.y + y);
                if (!visitedGrids.ContainsKey(checkGrid)) count++;
            }
        }
        return count;
    }

    public Vector2Int WorldToGrid(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x / gridSize), Mathf.RoundToInt(pos.z / gridSize));
    public Vector3 GridToWorld(Vector2Int grid) => new Vector3(grid.x * gridSize, 0, grid.y * gridSize);

    void OnDrawGizmos()
    {
        if (visitedGrids == null || visitedGrids.Count == 0) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        foreach (var grid in visitedGrids.Keys)
        {
            Gizmos.DrawCube(GridToWorld(grid), new Vector3(gridSize * 0.9f, 0.1f, gridSize * 0.9f));
        }
    }
}