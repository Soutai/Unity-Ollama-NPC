using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    private NPCNeeds needs;

    [Header("移动设置")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 3.5f;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private string currentExecutingAction;
    private GameObject currentExecutingTarget;

    // --- 修改：用于物理位移的方向记录 ---
    private Vector3 lastForwardDirection = Vector3.forward;
    // --- 新增：用于存储“探路大意图”的方向基准，不受局部绕路影响 ---
    private Vector3 explorationBaseDir = Vector3.forward;

    void Awake()
    {
        brain = GetComponent<NPCUtilityBrain>();
        needs = GetComponent<NPCNeeds>();
    }

    void Update()
    {
        GameObject targetObj = brain.GetCurrentTarget();
        string action = brain.currentAction.actionName;

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

    void PlanNewPath(string action, GameObject target)
    {
        waypoints.Clear();
        currentWaypointIndex = 0;
        Vector3 destination = transform.position;

        if (target != null)
        {
            destination = target.transform.position;
        }
        else if (action == "探索新区域")
        {
            // --- 策略：基于大意图方向旋转，而非实时物理方向 ---[cite: 23]
            Vector3 forward = explorationBaseDir;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;

            // 在意图方向正前方 [-30°, 30°] 随机旋转[cite: 23]
            float randomAngle = Random.Range(-30f, 30f);
            Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
            Vector3 searchDirection = (rotation * forward).normalized;

            float distance = Random.Range(40f, 60f);
            destination = transform.position + searchDirection * distance;

            // 只有确定了新的探路路径，才更新意图基准[cite: 23]
            explorationBaseDir = searchDirection;
            lastForwardDirection = searchDirection;
        }
        else // 闲逛
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            destination = transform.position + new Vector3(randomDir.x, 0, randomDir.y) * Random.Range(10f, 15f);
            // 闲逛不更新意图基准
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 20f, NavMesh.AllAreas))
        {
            destination = hit.position;
        }

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
        {
            if (path.status != NavMeshPathStatus.PathInvalid)
            {
                waypoints.AddRange(path.corners);
                return;
            }
        }

        FinishTask();
    }

    void ExecuteMovement(string action, GameObject target)
    {
        float speed = (action == "闲逛") ? walkSpeed : runSpeed;

        if (currentWaypointIndex < waypoints.Count)
        {
            Vector3 targetPoint = waypoints[currentWaypointIndex];
            targetPoint.y = transform.position.y;

            // --- 逻辑保护：探索时，不要让局部的绕路位移干扰大方向记录 ---[cite: 23]
            Vector3 moveDir = (targetPoint - transform.position).normalized;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                // 只有非探索状态（如闲逛）才让位移实时影响 lastForwardDirection
                // 这样在探索结束瞬间，lastForwardDirection 依然是大意图的方向[cite: 23]
                if (action != "探索新区域")
                {
                    lastForwardDirection = moveDir;
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPoint) < 0.2f)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            if (target == null)
            {
                FinishTask();
            }
            else
            {
                Vector3 dirToTarget = (target.transform.position - transform.position);
                dirToTarget.y = 0;
                if (dirToTarget.magnitude > 0.3f)
                {
                    transform.position += dirToTarget.normalized * speed * Time.deltaTime;
                }
            }
        }
    }

    void CheckInteraction(GameObject target)
    {
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.transform.position);
        float limit = target.CompareTag("Tree") ? 1.8f : 0.8f;

        if (dist <= limit)
        {
            if (target.CompareTag("Apple"))
            {
                needs.ApplyEat(20f);
                Destroy(target);
                FinishTask();
            }
            else if (target.CompareTag("Tree"))
            {
                TreeInteract tree = target.GetComponent<TreeInteract>();
                if (tree != null && tree.hasApples)
                {
                    tree.Interact();
                }
                FinishTask();
            }
        }
    }

    void FinishTask()
    {
        currentExecutingAction = "";
        currentExecutingTarget = null;
        waypoints.Clear();
        brain.ResetAction(); // 触发大脑重新 Think[cite: 20]
    }
}