using UnityEngine;

public class HungerSystem : MonoBehaviour
{
    [Header("饥饿设置")]
    public float maxHunger = 100f;
    public float currentHunger = 100f;
    public float hungerDrainRate = 2f; // 每秒消耗速度

    void Update()
    {
        if (currentHunger > 0)
        {
            currentHunger -= hungerDrainRate * Time.deltaTime;
        }
    }

    // 提供一个公共方法，让果子调用
    public void RestoreHunger(float amount)
    {
        currentHunger = Mathf.Min(currentHunger + amount, maxHunger);
        Debug.Log($"回复了 {amount} 点饥饿，当前：{currentHunger}");
    }
}