using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start() => mainCamera = Camera.main;

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // 1. 保持面向相机
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);

        // 2. 彻底解决父物体 Scale 为负导致的镜像问题
        Vector3 worldScale = transform.lossyScale;
        if (worldScale.x < 0 || worldScale.y < 0 || worldScale.z < 0)
        {
            // 如果全局缩放有负数，说明被父物体镜像了，我们通过 localScale 抵消它
            Vector3 local = transform.localScale;
            transform.localScale = new Vector3(
                Mathf.Abs(local.x) * (transform.parent.lossyScale.x < 0 ? -1 : 1),
                local.y,
                local.z
            );
        }
    }
}