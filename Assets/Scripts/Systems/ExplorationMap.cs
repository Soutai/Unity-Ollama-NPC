using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class ExplorationMap : MonoBehaviour
{
    public static ExplorationMap Instance;
    public float gridSize = 1.0f;
    private Dictionary<Vector2Int, bool> visitedGrids = new Dictionary<Vector2Int, bool>();

    // 调试数据
    private struct SampleDebug { public Vector3 dir; public float score; public bool isBest; }
    private List<SampleDebug> debugSamples = new List<SampleDebug>();
    private Transform npcTransform; // 引用NPC，用于实时跟随

    void Awake() => Instance = this;

    // 让NPC注册自己，方便圈圈跟着走
    public void RegisterNPC(Transform npc) => npcTransform = npc;

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

    public Vector3 GetUnexploredPoint(Vector3 currentPos, float detectionRadius)
    {
        debugSamples.Clear();
        Vector3 bestDirection = Vector3.zero;
        float maxScore = -1;

        for (int i = 0; i < 32; i++)
        {
            float angle = i * (Mathf.PI * 2 / 32);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));

            // 物理检查：确保目标点在NavMesh内[cite: 10]
            Vector3 checkPoint = currentPos + direction * detectionRadius;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(checkPoint, out hit, 3.0f, NavMesh.AllAreas)) continue;

            int density = CountUnexploredInRing(currentPos, direction, 12f, detectionRadius);
            float score = density + Random.Range(0f, 0.1f);

            debugSamples.Add(new SampleDebug { dir = direction, score = score, isBest = false });
            if (score > maxScore) { maxScore = score; bestDirection = direction; }
        }

        // 高亮最优方向[cite: 10]
        for (int i = 0; i < debugSamples.Count; i++)
        {
            if (debugSamples[i].dir == bestDirection)
            {
                var s = debugSamples[i]; s.isBest = true; debugSamples[i] = s;
            }
        }

        return currentPos + bestDirection * detectionRadius;
    }

    private int CountUnexploredInRing(Vector3 origin, Vector3 dir, float minR, float maxR)
    {
        int count = 0;
        for (float d = minR; d <= maxR; d += gridSize)
        {
            if (!visitedGrids.ContainsKey(WorldToGrid(origin + dir * d))) count++;
        }
        return count;
    }

    public bool IsVisited(Vector3 worldPos) => visitedGrids.ContainsKey(WorldToGrid(worldPos));
    public Vector2Int WorldToGrid(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x / gridSize), Mathf.RoundToInt(pos.z / gridSize));
    public Vector3 GridToWorld(Vector2Int grid) => new Vector3(grid.x * gridSize, 0, grid.y * gridSize);

    void OnDrawGizmos()
    {
        if (visitedGrids != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.1f);
            foreach (var grid in visitedGrids.Keys)
                Gizmos.DrawCube(GridToWorld(grid), new Vector3(gridSize, 0.1f, gridSize));
        }

        // 核心修正：使用 npcTransform.position 实现实时跟随显示
        if (npcTransform == null || debugSamples.Count == 0) return;
        Vector3 currentPos = npcTransform.position;

        foreach (var sample in debugSamples)
        {
            if (sample.isBest) Gizmos.color = Color.yellow;
            else Gizmos.color = Color.Lerp(Color.blue, Color.cyan, sample.score / 5f);

            Vector3 start = currentPos + sample.dir * 12f;
            Vector3 end = currentPos + sample.dir * 16f;
            Gizmos.DrawLine(start, end);
            if (sample.score > 0) Gizmos.DrawSphere(end, 0.1f * sample.score);
        }
    }
}