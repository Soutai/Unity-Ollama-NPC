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
        // 1. 实时标记地图
        if (ExplorationMap.Instance != null)
            ExplorationMap.Instance.MarkAsVisited(transform.position, visionRadius);

        GameObject targetObj = brain.GetCurrentTarget();
        string action = brain.currentAction.actionName;

        // 2. 核心逻辑：如果我们正在“探索”，但当前脚下已经涂过了，就考虑重新规划
        if (action.Contains("探索") && targetObj == null)
        {
            // 如果离目的地很近了，或者当前位置已经探索完毕，强制重新找点
            if (Vector3.Distance(transform.position, waypoints[waypoints.Count - 1]) < 2f)
            {
                PlanNewPath(action, null);
            }
        }

        // 状态切换检测（保持不变）
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
        else if (action.Contains("探索") || action.Contains("寻找"))
        {
            // 向地图系统申请未探索点
            destination = ExplorationMap.Instance.GetUnexploredPoint(transform.position, 35f);
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

    // --- 在 Scene 视图绘制 12f 的感知圆圈 ---
    void OnDrawGizmos()
    {
        Gizmos.color = (needs != null && needs.hunger < 60f) ? Color.red : Color.green;

        // 绘制感知圆圈线框
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