using UnityEngine;

// 1. 删除了 [RequireComponent(typeof(NavMeshAgent))]，解除强制依赖
public class NPCAnimationBridge : MonoBehaviour
{
    private NPCMotor motor; // 引用新的位移组件
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("平滑设置")]
    public float acceleration = 10f;
    private float currentAnimSpeed = 0f;

    // 用于通过位移计算速度
    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        // 获取新的 Motor 组件
        motor = GetComponent<NPCMotor>();

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null) Debug.LogWarning($"{gameObject.name} 找不到 Animator 组件！");
        if (spriteRenderer == null) Debug.LogWarning($"{gameObject.name} 找不到 SpriteRenderer 组件！");

        lastPosition = transform.position;
    }

    void Update()
    {
        // 实时计算当前帧的物理速度
        CalculateManualVelocity();

        if (animator == null) return;

        UpdateAnimation();
    }

    void CalculateManualVelocity()
    {
        // 通过位置差计算速度向量
        Vector3 delta = transform.position - lastPosition;
        currentVelocity = delta / Time.deltaTime;
        lastPosition = transform.position;
    }

    void UpdateAnimation()
    {
        // 只看 X-Z 平面的移动速度
        float groundSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;

        if (groundSpeed > 0.1f)
        {
            Vector3 dir = currentVelocity.normalized;

            // 1. 处理镜像逻辑：通过速度方向决定朝向
            if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.1f)
            {
                spriteRenderer.flipX = (dir.x < 0);
            }

            // 2. 传递混合树参数
            animator.SetFloat("Horizontal", dir.x);
            animator.SetFloat("Vertical", dir.z);

            // 3. 匹配速度阈值：直接根据实时 groundSpeed 判定
            // 如果速度接近 motor 设置的 runSpeed (3.5)，则切换到 Run 状态 (2.0)
            float targetValue = (groundSpeed > 2.5f) ? 2f : 1f;
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetValue, Time.deltaTime * acceleration);
        }
        else
        {
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, 0f, Time.deltaTime * acceleration);
        }

        animator.SetFloat("Speed", currentAnimSpeed);
    }
}