using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCAnimationBridge : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("平滑设置")]
    public float acceleration = 10f;
    private float currentAnimSpeed = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 尝试从自身或子物体获取组件
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 调试检查：如果还是找不到，在控制台报警
        if (animator == null) Debug.LogWarning($"{gameObject.name} 找不到 Animator 组件！");
        if (spriteRenderer == null) Debug.LogWarning($"{gameObject.name} 找不到 SpriteRenderer 组件！");
    }

    void Update()
    {
        // 安全检查：如果关键组件丢失，跳过逻辑，避免报错
        if (agent == null || animator == null) return;

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        Vector3 velocity = agent.velocity;
        float groundSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;

        if (groundSpeed > 0.1f)
        {
            Vector3 dir = velocity.normalized;

            // 统一镜像逻辑：仅使用 flipX，不改 localScale
            if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.1f)
            {
                spriteRenderer.flipX = (dir.x < 0);
            }

            // 确保参数传递给混合树[cite: 5]
            animator.SetFloat("Horizontal", dir.x);
            animator.SetFloat("Vertical", dir.z);

            // 匹配速度阈值[cite: 1, 5]
            float targetValue = (agent.speed > 3.0f) ? 2f : 1f;
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetValue, Time.deltaTime * acceleration);
        }
        else
        {
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, 0f, Time.deltaTime * acceleration);
        }

        animator.SetFloat("Speed", currentAnimSpeed);
    }
}