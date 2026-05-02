using UnityEngine;

public class PlayerMovement3D : MonoBehaviour
{
    [Header("移动速度设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float acceleration = 10f; // 动画参数切换的平滑度

    [Header("按键绑定")]
    public KeyCode runKey = KeyCode.LeftShift;

    private CharacterController controller;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector3 targetPosition;
    private bool isMouseMoving = false;
    private float currentAnimSpeed = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // 假设 Animator 和 SpriteRenderer 在子物体上 (处理 Billboard 的物体)
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        targetPosition = transform.position;
    }

    void Update()
    {
        // 1. 获取键盘输入向量[cite: 7]
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 keyboardDir = new Vector3(moveX, 0, moveZ).normalized;

        // 2. 获取跑步状态
        bool isRunning = Input.GetKey(runKey);
        float moveLimit = isRunning ? runSpeed : walkSpeed;

        // 3. 鼠标逻辑：点击地面更新目标点[cite: 7]
        if (Input.GetMouseButton(0))
        {
            UpdateMouseTarget();
        }

        // 4. 确定最终移动方向[cite: 7]
        Vector3 finalMoveDir = Vector3.zero;

        if (keyboardDir.magnitude > 0.1f)
        {
            isMouseMoving = false; // 键盘优先，打断鼠标移动
            finalMoveDir = keyboardDir;
        }
        else if (isMouseMoving)
        {
            Vector3 offset = targetPosition - transform.position;
            offset.y = 0;

            if (offset.magnitude > 0.2f)
                finalMoveDir = offset.normalized;
            else
                isMouseMoving = false;
        }

        // 5. 执行物理位移与动画同步
        HandleMovement(finalMoveDir, moveLimit);
        UpdateAnimation(finalMoveDir, isRunning);

        // 强制锁定 Y 轴，确保角色在地面上[cite: 7]
        LockYAxis();
    }

    void UpdateMouseTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            // 过滤：点击树木或玩家自己时不移动[cite: 7]
            if (hit.collider.CompareTag("Tree") || hit.collider.gameObject == gameObject)
                return;

            targetPosition = hit.point;
            isMouseMoving = true;
        }
    }

    void HandleMovement(Vector3 dir, float speed)
    {
        if (dir.magnitude > 0.1f)
        {
            controller.Move(dir * speed * Time.deltaTime);
        }
    }

    void UpdateAnimation(Vector3 dir, bool isRunning)
    {
        if (dir.magnitude > 0.1f)
        {
            // 镜像逻辑：只有在左右移动时翻转[cite: 7]
            if (dir.x != 0)
            {
                spriteRenderer.flipX = (dir.x < 0);
            }

            // 更新方向参数给 Blend Tree
            animator.SetFloat("Horizontal", dir.x);
            animator.SetFloat("Vertical", dir.z);

            // 计算目标速度值：1=走, 2=跑
            float targetValue = isRunning ? 2f : 1f;
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetValue, Time.deltaTime * acceleration);
        }
        else
        {
            // 停止时，平滑回到 Idle (0)
            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, 0f, Time.deltaTime * acceleration);
        }

        animator.SetFloat("Speed", currentAnimSpeed);
    }

    void LockYAxis()
    {
        if (Mathf.Abs(transform.position.y) > 0.01f)
        {
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        }
    }
}