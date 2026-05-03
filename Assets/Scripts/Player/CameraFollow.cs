using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    // 建议 offset 为 (0, 15, -10)，并在 Camera 旋转 X 轴约 50-60 度[cite: 3]
    public Vector3 offset = new Vector3(0, 15, -10);

    void LateUpdate()
    {
        if (target != null)
        {
            // 3D 空间平滑跟随目标[cite: 3]
            transform.position = target.position + offset;
        }
    }
}