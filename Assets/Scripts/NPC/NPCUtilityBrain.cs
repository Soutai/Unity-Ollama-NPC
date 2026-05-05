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
        bool isExecutingLongTask = (currentAction.actionName == "探索新区域" ||
                                    currentAction.actionName == "去捡苹果" ||
                                    currentAction.actionName == "去摇树");

        if (isExecutingLongTask && !string.IsNullOrEmpty(currentAction.actionName))
        {
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
            if (!foundInstantApple) return;
        }
        // ============================================================

        string currentStatusText = (needs.hunger < 60f) ? "饿了，正在寻找食物..." : "现在不饿";

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
                // --- 核心修改：记忆检索逻辑 ---
                // 如果附近没看到的，先问记忆系统有没有知道的果树[cite: 21]
                GameObject rememberedTree = GetComponent<NPCMemory>().GetBestRememberedTree();
                if (rememberedTree != null)
                {
                    best = new ActionChoice
                    {
                        actionName = "去摇树",
                        score = 35f, // 优先级：直接捡苹果 > 摇记忆中的树 > 盲目探索[cite: 19]
                        target = rememberedTree,
                        motive = "去记忆中的果树看看"
                    };
                }
                else
                {
                    best = new ActionChoice { actionName = "探索新区域", score = 20f, motive = "附近没吃的，记忆里也没有，去远处看看" };
                }
            }
            else
            {
                foreach (GameObject obj in nearby)
                {
                    float score = CalculateScore(obj);
                    if (obj == currentAction.target) score += PERSISTENCE_BONUS;

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