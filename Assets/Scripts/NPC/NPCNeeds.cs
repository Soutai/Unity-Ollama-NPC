using UnityEngine;
using System.Collections.Generic;

public class NPCNeeds : MonoBehaviour
{
    [Header("欲望指标 (0-100)")]
    public float hunger = 100f;
    public float hungerDecayRate = 1.2f;
    [Header("感知设置")]
    public float sightRange = 12f;
    public LayerMask interactableLayers; // 在 Inspector 勾选包含 Apple 和 Tree 的层级（比如 Default 和 Tree）

    void Update()
    {
        if (hunger > 0) hunger -= hungerDecayRate * Time.deltaTime;
    }

    // 感官：扫描指定层级内的物体
    public List<GameObject> GetNearbyObjects()
    {
        List<GameObject> detected = new List<GameObject>();
        // 强制使用 3D 扫描（如果你用的是 Sphere Collider）
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, interactableLayers);

        if (hits.Length == 0)
        {
            // 如果雷达空了，在控制台报个信
            // Debug.Log("雷达扫描中... 但附近啥也没摸着"); 
        }
        else
        {
            foreach (var hit in hits)
            {
                Debug.Log("雷达发现目标：" + hit.gameObject.name + " Tag是：" + hit.tag);
                detected.Add(hit.gameObject);
            }
        }
        return detected;
    }

    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0, 100);
    }
}