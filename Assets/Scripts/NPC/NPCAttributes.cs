using UnityEngine;

public class NPCAttributes : MonoBehaviour
{
    [Header("饥饿值设置")]
    public float hunger = 100f;
    public float hungerDecreaseRate = 2f; // 每秒扣多少
    public float hungerThreshold = 50f;   // 低于这个值开始找吃的

    void Update()
    {
        // 持续消耗饥饿值
        if (hunger > 0)
            hunger -= hungerDecreaseRate * Time.deltaTime;
    }

    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0, 100);
        Debug.Log($"NPC 吃了东西，当前饥饿值: {hunger}");
    }
}