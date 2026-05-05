using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    public float walkSpeed = 2.0f;
    public float visionRadius = 12f;
    public float detectionRadius = 16f;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;

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

        string action = (brain.currentAction.actionName != null) ? brain.currentAction.actionName : "";
        GameObject targetObj = brain.GetCurrentTarget();

        // 核心修正：只要没目标（比如处于“不饿”闲逛状态），就强制进行环带探索[cite: 9]
        if (targetObj == null)
        {
            if (waypoints.Count == 0)
            {
                PlanNewPath("探索", null);
            }
            else
            {
                // 如果终点已经被探索过了，或者快走到了，提前换个方向[cite: 9]
                Vector3 dest = waypoints[waypoints.Count - 1];
                if (ExplorationMap.Instance.IsVisited(dest) || Vector3.Distance(transform.position, dest) < 4f)
                {
                    PlanNewPath("探索", null);
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
        Vector3 targetPoint = waypoints[currentWaypointIndex];
        targetPoint.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, walkSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPoint) < 0.2f) currentWaypointIndex++;
    }
}