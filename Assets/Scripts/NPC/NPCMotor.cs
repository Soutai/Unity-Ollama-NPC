using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCMotor : MonoBehaviour
{
    private NPCUtilityBrain brain;
    private NPCNeeds needs;

    [Header("移动设置")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 4.0f; // 跑速已调高

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
    }

    void Update()
    {
        GameObject targetObj = brain.GetCurrentTarget();
        string action = brain.currentAction.actionName;

        // 如果目标在外部被销毁，强制重置执行状态
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

    void PlanNewPath(string action, GameObject target)
    {
        waypoints.Clear();
        currentWaypointIndex = 0;
        Vector3 destination = transform.position;

        if (target != null)
        {
            destination = target.transform.position;
        }
        else if (action.Contains("探索") || action.Contains("寻找")) // 模糊匹配
        {
            float angleRange = (Random.value > 0.85f) ? 30f : 5f;
            // --- 修正后的行 ---
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
        // 核心速度逻辑：根据饥饿值或是否有目标强制切换跑速
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
        float limit = target.CompareTag("Tree") ? 2.0f : 1.0f; // 宽松判定

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
                else
                {
                    interactionDone = true;
                }
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