using UnityEngine;
using System.Collections.Generic;

public class NPCNeeds : MonoBehaviour
{
    [Header("生理数值")]
    public float hunger = 100.0f;
    public float hungerDecayRate = 1.2f;

    [Header("感官设置")]
    public float sightRange = 12.0f;
    public LayerMask interactableLayers;

    [Header("UI 状态描述")]
    // 由决策大脑实时更新，供右下角 UI 读取
    public string currentAction = "初始化...";

    void Update()
    {
        // 持续消耗饥饿值[cite: 8, 10]
        if (hunger > 0)
        {
            hunger -= hungerDecayRate * Time.deltaTime;
        }
    }

    // 被动接口：仅由 Behavior 类在确认碰撞正确后调用[cite: 8]
    public void ApplyEat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0, 100);
    }

    // 感官接口：供 Brain 类调用以获取决策素材[cite: 8, 10]
    public List<GameObject> GetNearbyObjects()
    {
        List<GameObject> detected = new List<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, interactableLayers);
        foreach (var hit in hits) { detected.Add(hit.gameObject); }
        return detected;
    }
}