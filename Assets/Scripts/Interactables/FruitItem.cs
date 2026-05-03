using UnityEngine;

public class FruitItem : MonoBehaviour
{
    public float nutrition = 25f;
    public float pickupRadius = 1.0f;

    void Update()
    {
        // --- 逻辑 1：供 AI (Player_AI) 使用的主动检测 ---
        // AI 并不触发碰撞，而是靠这个半径检测来“吸取”果子
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player_AI"))
            {
                // 获取 AI 的需求脚本
                var npcNeeds = hit.GetComponent<NPCNeeds>();
                if (npcNeeds != null)
                {
                    npcNeeds.Eat(nutrition); // AI 确实用的是 Eat
                    Destroy(gameObject);
                    break;
                }
            }
        }
    }

    // --- 逻辑 2：供 玩家 (Player) 使用的碰撞检测 ---
    // 必须确保 Fruit_Prefab 的 Collider 勾选了 Is Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取 Player 的饥饿系统
            HungerSystem hunger = other.GetComponent<HungerSystem>();
            if (hunger != null)
            {
                // 修正这里：调用你 HungerSystem 里定义的 RestoreHunger
                hunger.RestoreHunger(nutrition);
                Destroy(gameObject);
            }
        }
    }
}