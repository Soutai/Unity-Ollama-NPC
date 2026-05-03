using UnityEngine;
using System;

public class TreeInteract : MonoBehaviour
{
    [Header("Settings")]
    public GameObject applePrefab;
    public Sprite appleTreeSprite;
    public Sprite emptyTreeSprite;
    public Transform dropPoint;

    public static event Action<string> OnAnyTreeShaken;

    private SpriteRenderer sr;
    private int applesLeft;
    // 暴露给 AI 检查的属性
    public bool hasApples { get; private set; } = true;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        // 随机 3 个的设定
        applesLeft = UnityEngine.Random.Range(1, 4);
        if (dropPoint == null) dropPoint = transform.Find("Drop Point") ?? transform;
    }

    // 修改：让方法返回布尔值，方便 AI 判断
    public bool Interact()
    {
        if (!hasApples) return false;

        DropApple();
        OnAnyTreeShaken?.Invoke(gameObject.name);
        return true;
    }

    void DropApple()
    {
        if (applePrefab != null)
        {
            Instantiate(applePrefab, dropPoint.position, Quaternion.identity);
        }

        if (--applesLeft <= 0)
        {
            hasApples = false;
            // 切换为没苹果的树皮
            if (sr && emptyTreeSprite) sr.sprite = emptyTreeSprite;
        }
    }

    void OnMouseDown() => Interact();
}