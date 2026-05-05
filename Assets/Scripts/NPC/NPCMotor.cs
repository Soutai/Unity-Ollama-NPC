using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    private NPCNeeds needs;

    [Header("移动速度")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f;

    [Header("感知半径设定")]
    public float visionRadius = 12f;      // 红色视线圈
    public float detectionRadius = 16f;   // 蓝色探测近圈
    public float farLookRadius = 24f;     // 深蓝色远眺圈（确保能跳出红区）

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private string currentExecutingAction;
    private GameObject currentExecutingTarget;

    void Awake()
    {
        brain = GetComponent<NPCUtilityBrain>();
        needs = GetComponent<NPCNeeds>();
    }

    void Update()
    {
        // 1. 实时标记已探索区域
        if (ExplorationMap.Instance != null)
            ExplorationMap.Instance.MarkAsVisited(transform.position, visionRadius);

        GameObject targetObj = brain.GetCurrentTarget();
        string action = (brain.currentAction.actionName != null) ? brain.currentAction.actionName : "";

        // 2. 探索重规划：目的地不再是“迷雾区”或者快走到了，就立即刷新航向
        if ((action.Contains("探索") || action.Contains("寻找")) && targetObj == null && waypoints.Count > 0)
        {
            Vector3 finalDest = waypoints[waypoints.Count - 1];
            // 提高灵敏度：距离终点 6f 时就开始寻找下一片绿地
            if (ExplorationMap.Instance.IsVisited(finalDest) || Vector3.Distance(transform.position, finalDest) < 6f)
            {
                PlanNewPath(action, null);
            }
        }

        // 3. 状态变更检测
        if (action != currentExecutingAction || targetObj != currentExecutingTarget)
        {
            PlanNewPath(action, targetObj);
            currentExecutingAction = action;
            currentExecutingTarget = targetObj;
        }

        ExecuteMovement(targetObj);
        if (targetObj != null) CheckInteraction(targetObj);
    }

    // --- NPCMotor.cs 修正部分 ---

    void PlanNewPath(string action, GameObject target)
    {
        waypoints.Clear();
        currentWaypointIndex = 0;
        Vector3 destination = transform.position;

        if (target != null)
        {
            destination = target.transform.position;
        }
        else if (action.Contains("探索") || action.Contains("寻找") || action.Contains("觅"))
        {
            destination = ExplorationMap.Instance.GetUnexploredPoint(transform.position, detectionRadius);

            // 强保：将采样点严丝合缝地贴合到最近的 NavMesh 面上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, 10.0f, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            Debug.DrawLine(transform.position, destination, Color.blue, 3f);
        }
        else
        {
            Vector2 rnd = Random.insideUnitCircle * 10f;
            destination = transform.position + new Vector3(rnd.x, 0, rnd.y);
        }

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
        {
            // 只有路径合法时才添加航点
            if (path.status != NavMeshPathStatus.PathInvalid)
            {
                waypoints.AddRange(path.corners);
            }
        }

        // --- 修正点：移除了之前强制 Add(destination) 的保底代码 ---
        // 那个代码会导致 NPC 忽略 NavMesh 直接直线穿墙或出界

        if (waypoints.Count == 0 && target == null) FinishTask();
    }

    void ExecuteMovement(GameObject target)
    {
        float speed = (target != null || (needs != null && needs.hunger < 60f)) ? runSpeed : walkSpeed;

        if (currentWaypointIndex < waypoints.Count)
        {
            Vector3 targetPoint = waypoints[currentWaypointIndex];
            targetPoint.y = transform.position.y;

            // 保持原始缩放，仅进行位置平移
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPoint) < 0.2f) currentWaypointIndex++;
        }
        else if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
        }
        else FinishTask();
    }

    void CheckInteraction(GameObject target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);
        float limit = target.CompareTag("Tree") ? 2.0f : 1.0f;

        if (dist <= limit)
        {
            if (target.CompareTag("Apple")) { needs.ApplyEat(20f); Destroy(target); }
            else if (target.CompareTag("Tree")) { var tree = target.GetComponent<TreeInteract>(); if (tree != null) tree.Interact(); }
            FinishTask();
        }
    }

    void FinishTask() { currentExecutingAction = ""; currentExecutingTarget = null; brain.ResetAction(); }

    void OnDrawGizmos()
    {
        // 1. 绘制 12f 视线圈（绿色/红色表示饥饿状态）
        Gizmos.color = (needs != null && needs.hunger < 60f) ? Color.red : Color.green;
        DrawCircleGizmo(transform.position, visionRadius);

        // 2. 绘制 16f 探测近圈（蓝色）
        Gizmos.color = Color.blue;
        DrawCircleGizmo(transform.position, detectionRadius);

        // 3. 绘制 24f 远眺圈（浅蓝色）
        // 这个圈负责捕捉红区外的远端绿地
        Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
        DrawCircleGizmo(transform.position, farLookRadius);
    }

    void DrawCircleGizmo(Vector3 center, float radius)
    {
        float segments = 64;
        Vector3 lastPos = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            Vector3 nextPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPos, nextPos);
            lastPos = nextPos;
        }
    }
}