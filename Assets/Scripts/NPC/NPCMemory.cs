using UnityEngine;
using System.Collections.Generic;

public class NPCMemory : MonoBehaviour
{
    public List<GameObject> knownTrees = new List<GameObject>();

    public void RecordTree(GameObject tree)
    {
        if (!knownTrees.Contains(tree)) knownTrees.Add(tree);
    }

    // 核心逻辑：提供一个有果子且最近的树
    public GameObject GetBestRememberedTree()
    {
        GameObject bestTree = null;
        float minDist = float.MaxValue;

        // 倒序遍历方便安全移除空引用
        for (int i = knownTrees.Count - 1; i >= 0; i--)
        {
            if (knownTrees[i] == null)
            {
                knownTrees.RemoveAt(i);
                continue;
            }

            TreeInteract treeScript = knownTrees[i].GetComponent<TreeInteract>();
            // 修改点：如果果树现在没有果子，直接跳过看下一棵，不删除记忆
            if (treeScript != null && treeScript.hasApples)
            {
                float d = Vector3.Distance(transform.position, knownTrees[i].transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    bestTree = knownTrees[i];
                }
            }
        }
        return bestTree;
    }
}