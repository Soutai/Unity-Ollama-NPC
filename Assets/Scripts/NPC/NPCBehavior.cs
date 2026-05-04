using UnityEngine;

public class NPCBehavior : MonoBehaviour
{
    private NPCNeeds needs;
    private NPCUtilityBrain brain;

    void Awake()
    {
        needs = GetComponent<NPCNeeds>();
        brain = GetComponent<NPCUtilityBrain>();
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        string currentTask = brain.currentAction.actionName;

        // 验证：大脑目标是捡苹果，且撞到的确实有 FruitItem 组件
        if (currentTask == "去捡苹果")
        {
            FruitItem fruit = other.GetComponent<FruitItem>();

            if (fruit != null)
            {
                // 核心重构：动态读取果子的营养属性，而不是写死 20f[cite: 14, 18]
                needs.ApplyEat(fruit.nutrition);

                Debug.Log($"NPC吃掉了{other.name}，回复了{fruit.nutrition}点饥饿值");

                Destroy(other);
                brain.ResetAction();
            }
        }
        // 摇树逻辑保持不变
        else if (currentTask == "去摇树" && other.CompareTag("Tree"))
        {
            TreeInteract tree = other.GetComponent<TreeInteract>();
            if (tree != null && tree.hasApples)
            {
                tree.Interact();
                needs.ApplyEat(2f);
                brain.ResetAction();
            }
        }
    }
}