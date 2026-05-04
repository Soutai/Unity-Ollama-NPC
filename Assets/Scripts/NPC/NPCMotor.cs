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
    public float visionRadius = 12f;

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
        if (ExplorationMap.Instance != null)
            ExplorationMap.Instance.MarkAsVisited(transform.position, visionRadius);

        GameObject targetObj = brain.GetCurrentTarget();
        string action = (brain.currentAction.actionName != null) ? brain.currentAction.actionName : "";

        // 核心优化：针对探索/寻找行为的“趋势感知”重规划[cite: 7]
        if ((action.Contains("探索") || action.Contains("寻找")) && targetObj == null && waypoints.Count > 0)
        {
            Vector3 finalDest = waypoints[waypoints.Count - 1];

            // 1. 目的地校验：如果目的地已经落入红区，立即寻找新的迷雾方向[cite: 7]
            if (ExplorationMap.Instance.IsVisited(finalDest))
            {
                PlanNewPath(action, null);
            }

            // 2. 提前评估：距离终点 4f（略大于视野边界）时，提前计算下一个高密度未探知区域[cite: 7]
            // 这能让 NPC 的移动轨迹更连贯，始终朝着迷雾最浓的方向走
            if (Vector3.Distance(transform.position, finalDest) < 4f)
            {
                PlanNewPath(action, null);
            }
        }

        if (action != currentExecutingAction || targetObj != currentExecutingTarget)
        {
            PlanNewPath(action, targetObj);
            currentExecutingAction = action;
            currentExecutingTarget = targetObj;
        }

        ExecuteMovement(targetObj);
        if (targetObj != null) CheckInteraction(targetObj);
    }

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
            // 向地图系统申请未探索点（现在逻辑已改为在视野边缘外探测最高密度区）[cite: 7]
            destination = ExplorationMap.Instance.GetUnexploredPoint(transform.position, 35f);

            // 强化版调试蓝线：保持 5 秒，方便查看选定的迷雾目标[cite: 7]
            Debug.DrawLine(transform.position, destination, Color.blue, 5f);
            Debug.DrawRay(destination, Vector3.up * 10f, Color.blue, 5f);
        }
        else
        {
            Vector2 rnd = Random.insideUnitCircle * 10f;
            destination = transform.position + new Vector3(rnd.x, 0, rnd.y);
        }

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
        {
            waypoints.AddRange(path.corners);
        }
        if (waypoints.Count == 0 && target == null) FinishTask();
    }

    void ExecuteMovement(GameObject target)
    {
        float speed = (target != null || (needs != null && needs.hunger < 60f)) ? runSpeed : walkSpeed;

        if (currentWaypointIndex < waypoints.Count)
        {
            Vector3 targetPoint = waypoints[currentWaypointIndex];
            targetPoint.y = transform.position.y;

            // 关键：绝对不修改 Transform 的 Scale 或 Rotation，防止 NPC 变形[cite: 7]
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
        Gizmos.color = (needs != null && needs.hunger < 60f) ? Color.red : Color.green;
        Vector3 center = transform.position + Vector3.up * 0.1f;
        float segments = 32;
        float angle = 0f;
        Vector3 lastPos = center + new Vector3(visionRadius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            angle = i * 2 * Mathf.PI / segments;
            Vector3 nextPos = center + new Vector3(Mathf.Cos(angle) * visionRadius, 0, Mathf.Sin(angle) * visionRadius);
            Gizmos.DrawLine(lastPos, nextPos);
            lastPos = nextPos;
        }
    }
}