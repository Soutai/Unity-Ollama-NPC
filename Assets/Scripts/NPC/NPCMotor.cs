using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMotor : MonoBehaviour
{
    private NavMeshAgent agent;
    private NPCUtilityBrain brain;
    private NPCNeeds needs;
    private Animator anim;

    [Header("移动设置")]
    // 降低基础速度，1.5 - 2.0 比较像正常的走路速度
    public float walkSpeed = 2.0f;
    // 降低加速度，让 NPC 启动和停止时有缓冲，不那么突兀
    public float acceleration = 4.0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        brain = GetComponent<NPCUtilityBrain>();
        needs = GetComponent<NPCNeeds>();
        anim = GetComponent<Animator>();

        // 2D 基础设置
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // 在代码中初始化物理特性
        agent.speed = walkSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = 0; // 2D 旋转通常由 Scale 镜像控制，不需要 Agent 旋转[cite: 1, 2]
    }

    void Update()
    {
        // 1. 动画控制：根据 agent.velocity.magnitude 传值给 Speed 参数[cite: 1, 2]
        float currentSpeed = agent.velocity.magnitude;
        if (anim != null) anim.SetFloat("Speed", currentSpeed);

        // 2. 转向控制[cite: 1, 2]
        if (agent.velocity.x > 0.1f) transform.localScale = new Vector3(-1, 1.2f, 1);
        else if (agent.velocity.x < -0.1f) transform.localScale = new Vector3(1, 1.2f, 1);

        // 3. 执行决策
        GameObject targetObj = brain.GetCurrentTarget();
        if (targetObj != null)
        {
            MoveToTarget(targetObj);
            CheckInteraction(targetObj);
        }
        else
        {
            Wander();
        }
    }

    void MoveToTarget(GameObject target)
    {
        agent.SetDestination(target.transform.position);
        // 去目标点时使用标准走速
        agent.speed = walkSpeed;
        agent.stoppingDistance = target.CompareTag("Tree") ? 1.8f : 0.4f; 
    }

    void CheckInteraction(GameObject target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist <= agent.stoppingDistance + 0.3f)
        {
            if (target.CompareTag("Apple"))
            {
                needs.Eat(20f); 
                Destroy(target);
                brain.ResetAction();
            }
            else if (target.CompareTag("Tree"))
            {
                TreeInteract tree = target.GetComponent<TreeInteract>(); 
                if (tree != null && tree.hasApples)
                {
                    tree.Interact(); 
                }
                needs.Eat(2f);
                brain.ResetAction();
            }
        }
    }

    void Wander()
    {
        agent.stoppingDistance = 0f;
        // 漫游时更慢一点，像是在散步
        agent.speed = walkSpeed * 0.7f;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector2 randomDir = Random.insideUnitCircle * 4f;
            Vector3 wanderPos = transform.position + new Vector3(randomDir.x, randomDir.y, 0);
            agent.SetDestination(wanderPos);
        }
    }
}