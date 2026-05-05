using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    public float walkSpeed = 2.0f;
    public float runSpeed = 3.0f; // 新增：跑步速度
    public float visionRadius = 12f;
    public float detectionRadius = 16f;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;

    private GameObject lastTarget; // 新增：用于记录上一帧的目标

    void Awake()
    {
        brain = GetComponent<NPCUtilityBrain>();
    }

    void Start()
    {
        // 修正点：将自己注册给地图，让方向圈实时跟随[cite: 10]
        if (ExplorationMap.Instance != null)
            ExplorationMap.Instance.RegisterNPC(this.transform);
    }

    void Update()
    {
        if (ExplorationMap.Instance != null)
            ExplorationMap.Instance.MarkAsVisited(transform.position, visionRadius);

        GameObject targetObj = brain.GetCurrentTarget();

        // --- 核心修复：目标切换检测 ---
        // 如果这一帧的目标和上一帧不一样（比如从 null 变成了苹果），立即重新规划路径[cite: 13, 17]
        if (targetObj != lastTarget && targetObj != null)
        {
            PlanNewPath(brain.currentAction.actionName, targetObj);
        }
        lastTarget = targetObj; 
        
        // 更新记录
        // ----------------------------

        if (targetObj == null)
        {
            if (waypoints.Count == 0)
            {
                PlanNewPath("闲逛", null);
            }
            else
            {
                Vector3 dest = waypoints[waypoints.Count - 1];
                if (ExplorationMap.Instance.IsVisited(dest) || Vector3.Distance(transform.position, dest) < 4f)
                {
                    PlanNewPath("闲逛", null);
                }
            }
        }

        ExecuteMovement();
    }

    void PlanNewPath(string action, GameObject target)
    {
        waypoints.Clear();
        currentWaypointIndex = 0;
        Vector3 dest = (target != null) ? target.transform.position :
                       ExplorationMap.Instance.GetUnexploredPoint(transform.position, detectionRadius);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(dest, out hit, 10.0f, NavMesh.AllAreas)) dest = hit.position;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, dest, NavMesh.AllAreas, path))
        {
            if (path.status != NavMeshPathStatus.PathInvalid) waypoints.AddRange(path.corners);
        }
    }

    void ExecuteMovement()
    {
        if (currentWaypointIndex >= waypoints.Count) return;

        string action = (brain.currentAction.actionName != null) ? brain.currentAction.actionName : "";
        GameObject targetObj = brain.GetCurrentTarget();

        // --- 核心修复：针对“苹果”和“树”的通用交互衔接 ---
        if (targetObj != null)
        {
            float dist = Vector3.Distance(transform.position, targetObj.transform.position);

            // 1. 处理摇树逻辑 (距离 < 1.5f)[cite: 16]
            if (action == "去摇树" && dist < 1.5f)
            {
                TreeInteract tree = targetObj.GetComponent<TreeInteract>();
                if (tree != null && tree.hasApples)
                {
                    tree.Interact();
                    GetComponent<NPCNeeds>().ApplyEat(2f);
                    brain.ResetAction();
                    waypoints.Clear();
                    return;
                }
            }
            // 2. 处理捡苹果逻辑 (距离 < 0.8f，因为苹果更小，需要更近一些)
            else if (action == "去捡苹果" && dist < 0.8f)
            {
                FruitItem fruit = targetObj.GetComponent<FruitItem>();
                if (fruit != null)
                {
                    GetComponent<NPCNeeds>().ApplyEat(fruit.nutrition);
                    Debug.Log($"NPC主动交互吃掉了{targetObj.name}");
                    Destroy(targetObj);
                    brain.ResetAction();
                    waypoints.Clear();
                    return;
                }
            }
        }
        // ------------------------------------------------

        // 以下原有移动逻辑保持不变
        float currentSpeed = walkSpeed;
        bool isHungry = GetComponent<NPCNeeds>().hunger < 60f;

        if (action == "去捡苹果" || action == "去摇树")
        {
            currentSpeed = runSpeed;
        }
        else if (action == "闲逛" || action == "探索新区域" || action == "探索")
        {
            currentSpeed = isHungry ? runSpeed : walkSpeed;
        }

        Vector3 targetPoint = waypoints[currentWaypointIndex];
        targetPoint.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < 0.2f)
        {
            currentWaypointIndex++;
        }
    }
}