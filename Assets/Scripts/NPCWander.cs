using UnityEngine;
using UnityEngine.AI;

public class NPCWander : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim; // 新增动画组件引用
    public float wanderRadius = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>(); // 获取动画机
        InvokeRepeating("SetNewDestination", 0, 5f);
    }

    void Update()
    {
        // 核心逻辑：获取 agent 的当前速度大小
        // 如果速度 > 0.1，就说明在走，给动画机传值
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);

        // 镜像翻转（可选）：根据走路方向让纸片人左右转身
        if (agent.velocity.x > 0.1f) transform.localScale = new Vector3(-1, 1.2f, 1);
        else if (agent.velocity.x < -0.1f) transform.localScale = new Vector3(1, 1.2f, 1);
    }

    void SetNewDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}