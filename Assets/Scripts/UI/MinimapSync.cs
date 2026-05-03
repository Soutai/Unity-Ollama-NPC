using UnityEngine;

public class MinimapSync : MonoBehaviour
{
    [Header("绑定对象")]
    public Transform player;          // 拖入 Player 整体物体
    public Transform arrowIcon;       // 拖入 Player 下设为 Minimap 层的箭头图标
    public RectTransform northText;   // (可选) 拖入 UI 上的北方 N 标志

    [Header("高度设置")]
    public float cameraHeight = 20f;  // 小地图摄像机距离地面的高度

    private CharacterController playerController;

    void Start()
    {
        // 自动获取玩家的物理组件，用于读取实时速度
        if (player != null)
        {
            playerController = player.GetComponent<CharacterController>();
        }
    }

    // 使用 LateUpdate 确保在玩家移动完成后再更新相机位置，防止画面抖动[cite: 3]
    void LateUpdate()
    {
        if (player == null || arrowIcon == null) return;

        // 1. 相机跟随：保持在玩家正上方，坐标 Y 固定[cite: 3]
        transform.position = new Vector3(player.position.x, cameraHeight, player.position.z);

        // 2. 箭头旋转逻辑
        // 核心修改：通过读取 CharacterController 的 velocity（速度）来获取移动方向[cite: 5, 6]
        Vector3 moveVelocity = playerController.velocity;

        // 只有当玩家在移动时才更新角度，防止停止时旋转重置
        if (moveVelocity.magnitude > 0.1f)
        {
            // 在 XZ 平面计算角度
            // Mathf.Atan2(x, z) 返回弧度，乘以 Rad2Deg 转为角度
            float targetAngle = Mathf.Atan2(moveVelocity.x, moveVelocity.z) * Mathf.Rad2Deg;

            // 设置旋转：
            // X 轴保持 90 度（平贴地面）
            // Y 轴设为计算出的角度
            // Z 轴为 0
            arrowIcon.rotation = Quaternion.Euler(90, targetAngle, 0);
        }

        // 3. (可选) 北方标志逻辑
        // 如果你的小地图相机不旋转，北方标志通常固定在 UI 上即可。
        // 如果你以后想让地图跟着人转，这里需要额外逻辑。
    }
}