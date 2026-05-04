using UnityEngine;
using System.Collections.Generic;

public class ExplorationMap : MonoBehaviour
{
    public static ExplorationMap Instance;

    [Header("网格设置")]
    public float cellSize = 2.5f; // 步长越大，覆盖越快
    private Dictionary<Vector2Int, bool> visitedGrids = new Dictionary<Vector2Int, bool>();

    void Awake() { Instance = this; }

    public Vector2Int WorldToGrid(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.z / cellSize));

    // 像画笔一样标记已探索区域
    public void MarkAsVisited(Vector3 pos, float radius)
    {
        int range = Mathf.CeilToInt(radius / cellSize);
        Vector2Int center = WorldToGrid(pos);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int current = new Vector2Int(center.x + x, center.y + y);
                Vector3 gridWorldPos = new Vector3(current.x * cellSize, 0, current.y * cellSize);
                if (Vector3.Distance(pos, gridWorldPos) <= radius) visitedGrids[current] = true;
            }
        }
    }

    // 寻找最近的“处女地”
    public Vector3 GetUnexploredPoint(Vector3 currentPos, float maxSearchRadius)
    {
        Vector3 bestPoint = currentPos;
        int maxUnexploredCount = -1;

        // 定义探测环：在视野边缘（14f）进行采样
        float detectionRadius = 14f;
        int sampleCount = 12; // 采样 12 个方向，覆盖 360 度

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = i * (Mathf.PI * 2 / sampleCount);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 samplePos = currentPos + direction * detectionRadius;

            // 对每个采样点计算其周围的“迷雾密度”
            int unexploredDensity = CountUnexploredNearby(samplePos, 3); // 检查周围 3 单元格范围

            if (unexploredDensity > maxUnexploredCount)
            {
                maxUnexploredCount = unexploredDensity;
                bestPoint = samplePos;
            }
        }

        // 如果发现所有方向都探索过了，或者密度太低，则扩大搜索半径
        if (maxUnexploredCount <= 2)
        {
            // 这里的逻辑保持你之前的远距离保底搜索[cite: 4]
            return currentPos + new Vector3(Random.Range(-maxSearchRadius, maxSearchRadius), 0, Random.Range(-maxSearchRadius, maxSearchRadius));
        }

        // 为了让路径更自然，给选定的方向加一点随机偏移
        return bestPoint + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
    }

    // 辅助函数：计算某个点周围未探索格子的数量
    private int CountUnexploredNearby(Vector3 pos, int range)
    {
        int count = 0;
        Vector2Int centerGrid = WorldToGrid(pos);

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                Vector2Int checkGrid = new Vector2Int(centerGrid.x + x, centerGrid.y + y);
                if (!visitedGrids.ContainsKey(checkGrid))
                {
                    count++;
                }
            }
        }
        return count;
    }

    // 供外部判断某个点是否已经被探索过
    public bool IsVisited(Vector3 worldPos)
    {
        return visitedGrids.ContainsKey(WorldToGrid(worldPos));
    }

    // 调试用：在 Scene 视图画出已探索的格子
    // 把原来的 OnDrawGizmosSelected 改成 OnDrawGizmos
    void OnDrawGizmos()
    {
        if (visitedGrids == null) return;

        // 将颜色改为红色 (Red=1, Green=0, Blue=0)，Alpha设为 0.3 保持半透明
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        foreach (var grid in visitedGrids.Keys)
        {
            // 保持 0.05f 或 0.1f 的高度偏移，防止被地面遮挡[cite: 7]
            Vector3 pos = new Vector3(grid.x * cellSize, 0.1f, grid.y * cellSize);

            // 绘制红色方块
            Gizmos.DrawCube(pos, new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f));
        }
    }
}