using UnityEngine;
using System.Collections.Generic;

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
    public Vector3 GetUnexploredPoint(Vector3 currentPos, float detectionRadius)
    {
        Vector3 bestPoint = currentPos;
        float maxUnexploredCount = -1;

        // 增加一个“远眺”半径，确保能跳出红区
        float farLookRadius = detectionRadius + 8f; // 比如探测 24f 处

        int sampleCount = 32;
        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * (Mathf.PI * 2 / sampleCount);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // 同时检查 16f(近处) 和 24f(远处) 的平均密度，确保它能看到远方的空地[cite: 5]
            Vector3 nearPoint = currentPos + direction * detectionRadius;
            Vector3 farPoint = currentPos + direction * farLookRadius;

            int density = CountUnexploredNearby(nearPoint, 4) + CountUnexploredNearby(farPoint, 6);

            float score = density + Random.Range(0f, 0.5f);
            if (score > maxUnexploredCount)
            {
                maxUnexploredCount = score;
                bestPoint = farPoint; // 目标直接定在远处的绿地上[cite: 5]
            }
        }

        // 只有当远处也完全没绿地时，才随机乱走
        if (maxUnexploredCount <= 1)
        {
            return currentPos + new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
        }

        return bestPoint;
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