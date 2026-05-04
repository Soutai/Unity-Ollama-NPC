using UnityEngine;
using System.Collections.Generic;

public class NPCUtilityBrain : MonoBehaviour
{
    [System.Serializable]
    public struct ActionChoice
    {
        public string actionName;
        public float score;
        public GameObject target;
        public string motive;
    }

    private NPCNeeds needs;
    public ActionChoice currentAction;

    [Header("决策持久性设置")]
    [SerializeField] private float lastDecisionScore = 0f;
    [SerializeField] private const float PERSISTENCE_BONUS = 25f; // 保持稳定性奖励

    void Start()
    {
        needs = GetComponent<NPCNeeds>();
        InvokeRepeating("Think", 0f, 0.5f); // 每 0.5 秒思考一次
    }

    void Think()
    {
        // ================== 核心修改：行为锁定策略 ==================
        // 1. 检查是否正在执行“长距离执行”动作
        bool isExecutingLongTask = (currentAction.actionName == "探索新区域" ||
                                    currentAction.actionName == "去捡苹果" ||
                                    currentAction.actionName == "去摇树");

        // 2. 如果正在执行任务，且 Motor 还没通过 ResetAction() 清除它
        if (isExecutingLongTask && !string.IsNullOrEmpty(currentAction.actionName))
        {
            // 策略：排除杂念，纯粹探路。
            // 除非是极其紧急的情况（例如饥饿度掉到临界点且附近有直接能吃的苹果），否则不准打断。

            // 紧急任务检查：如果还没捡到苹果，但路边突然出现一个苹果
            List<GameObject> nearby = needs.GetNearbyObjects();
            bool foundInstantApple = false;
            foreach (GameObject obj in nearby)
            {
                if (obj.CompareTag("Apple") && currentAction.actionName != "去捡苹果")
                {
                    foundInstantApple = true;
                    break;
                }
            }

            // 如果没发现更紧急的即时食物，直接退出 Think，让 Motor 继续跑完当前路径
            if (!foundInstantApple) return;
        }
        // ============================================================

        string currentStatusText = (needs.hunger < 60f) ? "饿了，正在寻找食物..." : "现在不饿";

        // 只有在闲逛或探索时才更新基础状态文本
        if (currentAction.actionName == "闲逛" || currentAction.actionName == "探索新区域")
        {
            needs.currentAction = currentStatusText;
        }

        ActionChoice best = new ActionChoice { actionName = "闲逛", score = 10f, motive = currentStatusText };

        if (needs.hunger < 60f)
        {
            List<GameObject> nearby = needs.GetNearbyObjects();
            if (nearby.Count == 0)
            {
                // 如果正在探索，不重复发指令，由上面的锁定机制控制
                best = new ActionChoice { actionName = "探索新区域", score = 20f, motive = "附近没吃的，去远处看看" };
            }
            else
            {
                foreach (GameObject obj in nearby)
                {
                    float score = CalculateScore(obj);
                    if (obj == currentAction.target) score += PERSISTENCE_BONUS; //[cite: 19]

                    if (score > best.score)
                    {
                        best = new ActionChoice
                        {
                            actionName = obj.CompareTag("Apple") ? "去捡苹果" : "去摇树",
                            score = score,
                            target = obj,
                            motive = "发现食物！"
                        };
                    }
                }
            }
        }

        // 仅当新决策分值更高时才更新[cite: 19]
        if (best.score > lastDecisionScore || lastDecisionScore == 0)
        {
            currentAction = best;
            lastDecisionScore = best.score;
            needs.currentAction = best.motive;
        }
    }

    float CalculateScore(GameObject obj)
    {
        if (obj.CompareTag("Apple")) return 50f + (100f - needs.hunger);
        if (obj.CompareTag("Tree"))
        {
            TreeInteract tree = obj.GetComponent<TreeInteract>();
            if (tree != null && !tree.hasApples) return 0;
            return 30f + (100f - needs.hunger) * 0.5f;
        }
        return 0;
    }

    public void ResetAction()
    {
        currentAction = new ActionChoice { actionName = "", score = 0f, motive = "执行完成，等待观察" };
        lastDecisionScore = 0f; //[cite: 19]
    }

    public GameObject GetCurrentTarget() => currentAction.target;
}