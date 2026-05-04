using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public GameObject treePrefab;
    public int treeCount = 1;

    // 引用你的地面物体
    public Transform groundTransform;

    void Start() => GenerateWorld();

    void GenerateWorld()
    {
        GameObject treeContainer = new GameObject("Trees");

        // 自动获取地面的缩放值作为参考范围
        // 如果是 Plane，基础大小是 10x10，所以半径是 scale * 5
        float rangeX = groundTransform.localScale.x * 5f;
        float rangeZ = groundTransform.localScale.z * 5f;

        for (int i = 0; i < treeCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-rangeX, rangeX),
                0,
                Random.Range(-rangeZ, rangeZ)
            );

            Instantiate(treePrefab, randomPos, Quaternion.identity, treeContainer.transform);
        }
    }
}