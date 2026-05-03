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

    [TextArea(3, 5)]
    public string currentThought;

    void Start()
    {
        needs = GetComponent<NPCNeeds>();
        ResetAction();
        InvokeRepeating("Think", 0f, 0.5f);
    }

    void Think()
    {
        // 1. 默认：随便逛逛
        ActionChoice best = new ActionChoice
        {
            actionName = "随便逛逛",
            score = 10.0f,
            target = null,
            motive = "肚子还不饿，到处走走"
        };

        // 2. 只有饥饿度低于 60 才会考虑寻找食物
        bool isHungryEnough = needs.hunger < 60f;

        if (isHungryEnough)
        {
            List<GameObject> nearby = needs.GetNearbyObjects();

            foreach (GameObject obj in nearby)
            {
                if (obj == null) continue;
                float score = 0;

                if (obj.CompareTag("Apple"))
                {
                    // 饥饿度越低，分值越高
                    score = 50f + (100f - needs.hunger);
                    if (score > best.score)
                    {
                        best = new ActionChoice
                        {
                            actionName = "去捡苹果",
                            score = score,
                            target = obj,
                            motive = "有点饿了，去吃苹果"
                        };
                    }
                }
                else if (obj.CompareTag("Tree"))
                {
                    TreeInteract tree = obj.GetComponent<TreeInteract>();
                    if (tree != null && tree.hasApples)
                    {
                        score = 30f + (100f - needs.hunger) * 0.5f;
                        if (score > best.score)
                        {
                            best = new ActionChoice
                            {
                                actionName = "去摇树",
                                score = score,
                                target = obj,
                                motive = "存点粮食，以防万一"
                            };
                        }
                    }
                }
            }
        }
        else
        {
            // 如果不饿，强制保持动机为“不饿”
            best.motive = "现在不饿，不用找食物";
        }

        currentAction = best;
        UpdateDisplay(best);
    }

    void UpdateDisplay(ActionChoice choice)
    {
        currentThought = $"原因: {choice.motive}\n" +
                         $"分值: {choice.score:F1}\n" +
                         $"行动: {choice.actionName}";
    }

    public void ResetAction()
    {
        ActionChoice idle = new ActionChoice
        {
            actionName = "闲逛中",
            score = 10.0f,
            target = null,
            motive = "刚忙完，休息一下"
        };
        currentAction = idle;
        UpdateDisplay(idle);
    }

    public GameObject GetCurrentTarget() => currentAction.target;
}