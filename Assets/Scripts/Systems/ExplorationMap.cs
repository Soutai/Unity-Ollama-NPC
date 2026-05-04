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
        Vector2Int startGrid = WorldToGrid(currentPos);

        // 采用“螺旋搜索”或“同心圆采样”，由近及远查找
        // 第一层：在 5-15 米范围内找（近处开荒）
        // 第二层：在 15-max 范围内找（远方探索）
        float[] rings = { 10f, 20f, 35f, 50f };

        foreach (float radius in rings)
        {
            for (int i = 0; i < 20; i++) // 每个半径采样 20 次
            {
                float angle = Random.Range(0f, Mathf.PI * 2);
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 testPos = currentPos + offset;

                if (!visitedGrids.ContainsKey(WorldToGrid(testPos)))
                {
                    return testPos; // 发现未涂过的点，立刻返回
                }
            }
        }

        // 如果周围全涂过了，强制生成一个远处的随机点来打破僵局
        return currentPos + new Vector3(Random.Range(-50, 50), 0, Random.Range(-50, 50));
    }

    // 调试用：在 Scene 视图画出已探索的格子
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        foreach (var grid in visitedGrids.Keys)
        {
            Gizmos.DrawCube(new Vector3(grid.x * cellSize, 0, grid.y * cellSize), new Vector3(cellSize, 0.1f, cellSize));
        }
    }
}