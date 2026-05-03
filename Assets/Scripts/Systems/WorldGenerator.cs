using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public GameObject treePrefab;
    public int treeCount = 50;
    public Vector2 spawnRange = new Vector2(50, 50);

    void Start() => GenerateWorld();

    void GenerateWorld()
    {
        // 创建一个父物体以便管理 Hierarchy
        GameObject treeContainer = new GameObject("Trees");

        for (int i = 0; i < treeCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnRange.x, spawnRange.x),
                0,
                Random.Range(-spawnRange.y, spawnRange.y)
            );

            GameObject tree = Instantiate(treePrefab, randomPos, Quaternion.identity, treeContainer.transform);
            tree.name = $"Tree_{i}"; // 给予唯一名称方便调试
        }
    }
}