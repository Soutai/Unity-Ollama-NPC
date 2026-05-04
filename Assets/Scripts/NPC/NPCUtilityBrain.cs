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

    void Start()
    {
        needs = GetComponent<NPCNeeds>();
        InvokeRepeating("Think", 0f, 0.5f); // 每 0.5 秒思考一次[cite: 2, 10]
    }

    void Think()
    {
        ActionChoice best = new ActionChoice { actionName = "闲逛", score = 10f, motive = "现在不饿" };

        if (needs.hunger < 60f) // 触发寻找食物的阈值[cite: 2, 10]
        {
            List<GameObject> nearby = needs.GetNearbyObjects();
            foreach (GameObject obj in nearby)
            {
                if (obj == null) continue;
                float score = CalculateScore(obj);
                if (score > best.score)
                {
                    best = new ActionChoice
                    {
                        actionName = obj.CompareTag("Apple") ? "去捡苹果" : "去摇树",
                        score = score,
                        target = obj,
                        motive = "肚子饿了，去找点吃的"
                    };
                }
            }
        }
        currentAction = best;
        needs.currentAction = best.motive; // 同步给 Needs 供 UI 显示[cite: 10]
    }

    float CalculateScore(GameObject obj)
    {
        if (obj.CompareTag("Apple")) return 50f + (100f - needs.hunger);
        if (obj.CompareTag("Tree")) return 30f + (100f - needs.hunger) * 0.5f;
        return 0;
    }

    public void ResetAction() { currentAction = new ActionChoice { actionName = "闲逛", score = 10f }; }
    public GameObject GetCurrentTarget() => currentAction.target;
}