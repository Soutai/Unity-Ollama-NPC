using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// 确保物体上有 LineRenderer 组件
[RequireComponent(typeof(LineRenderer))]
public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    private NPCNeeds needs;
    private LineRenderer lineRenderer;

    [Header("移动设置")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f;

    [Header("可视化设置")]
    public float visionRadius = 12f; // 你的感知范围[cite: 11]
    public Color circleColor = Color.green;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private string currentExecutingAction;
    private GameObject currentExecutingTarget;
    private Vector3 explorationBaseDir = Vector3.forward;

    void Awake()
    {
        brain = GetComponent<NPCUtilityBrain>();
        needs = GetComponent<NPCNeeds>();
        explorationBaseDir = transform.forward;

        // 初始化可视化圆圈
        InitVisionCircle();
    }

    void InitVisionCircle()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false; // 关键：随 NPC 移动[cite: 11]
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.positionCount = 51; // 50段实现平滑圆弧
        lineRenderer.startColor = circleColor;
        lineRenderer.endColor = circleColor;

        // 预先计算圆圈坐标（局部坐标系）
        for (int i = 0; i <= 50; i++)
        {
            float angle = i * (2 * Mathf.PI / 50);
            float x = Mathf.Cos(angle) * visionRadius;
            float z = Mathf.Sin(angle) * visionRadius;
            lineRenderer.SetPosition(i, new Vector3(x, 0.1f, z)); // 稍微抬高 0.1f 防止跟地面闪烁
        }
    }

    void Update()
    {
        // 实时更新颜色（比如饿了变红）
        if (needs != null)
        {
            lineRenderer.startColor = needs.hunger < 60f ? Color.red : circleColor;
            lineRenderer.endColor = lineRenderer.startColor;
        }

        GameObject targetObj = brain.GetCurrentTarget();
        string action = brain.currentAction.actionName;

        if (currentExecutingTarget == null && targetObj != null)
        {
            currentExecutingAction = "";
        }

        if (action != currentExecutingAction || targetObj != currentExecutingTarget)
        {
            PlanNewPath(action, targetObj);
            currentExecutingAction = action;
            currentExecutingTarget = targetObj;
        }

        ExecuteMovement(action, targetObj);

        if (targetObj != null)
        {
            CheckInteraction(targetObj);
        }
    }

    // --- 以下逻辑保持 PlanNewPath, ExecuteMovement, CheckInteraction, FinishTask 不变 ---
    // (参考之前的代码版本)

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
            float angleRange = (Random.value > 0.85f) ? 30f : 5f;
            float randomAngle = Random.Range(-angleRange, angleRange);
            explorationBaseDir = Quaternion.Euler(0, randomAngle, 0) * explorationBaseDir;
            destination = transform.position + explorationBaseDir * Random.Range(50f, 80f);
        }
        else
        {
            Vector2 rnd = Random.insideUnitCircle.normalized;
            destination = transform.position + new Vector3(rnd.x, 0, rnd.y) * Random.Range(10f, 15f);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 10f, NavMesh.AllAreas))
            destination = hit.position;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
        {
            waypoints.AddRange(path.corners);
        }

        if (waypoints.Count == 0 && target == null) FinishTask();
    }

    void ExecuteMovement(string action, GameObject target)
    {
        float speed = (target != null || (needs != null && needs.hunger < 60f)) ? runSpeed : walkSpeed;

        if (currentWaypointIndex < waypoints.Count)
        {
            Vector3 targetPoint = waypoints[currentWaypointIndex];
            targetPoint.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint) < 0.2f)
                currentWaypointIndex++;
        }
        else if (target != null)
        {
            Vector3 dir = (target.transform.position - transform.position);
            dir.y = 0;
            if (dir.magnitude > 0.1f)
            {
                transform.position += dir.normalized * speed * Time.deltaTime;
            }
        }
        else
        {
            FinishTask();
        }
    }

    void CheckInteraction(GameObject target)
    {
        if (target == null) return;
        float dist = Vector3.Distance(transform.position, target.transform.position);
        float limit = target.CompareTag("Tree") ? 2.0f : 1.0f;

        if (dist <= limit)
        {
            bool interactionDone = false;
            if (target.CompareTag("Apple"))
            {
                needs.ApplyEat(20f);
                Destroy(target);
                currentExecutingTarget = null;
                interactionDone = true;
            }
            else if (target.CompareTag("Tree"))
            {
                TreeInteract tree = target.GetComponent<TreeInteract>();
                if (tree != null && tree.hasApples)
                {
                    tree.Interact();
                    interactionDone = true;
                }
                else interactionDone = true;
            }
            if (interactionDone) FinishTask();
        }
    }

    void FinishTask()
    {
        currentExecutingAction = "";
        currentExecutingTarget = null;
        waypoints.Clear();
        if (brain != null) brain.ResetAction();
    }
}