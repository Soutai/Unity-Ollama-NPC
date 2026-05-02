using UnityEngine;
using System;

public class TreeInteract : MonoBehaviour
{
    [Header("Settings")]
    public GameObject applePrefab;
    public Sprite appleTreeSprite;
    public Sprite emptyTreeSprite;
    public Transform dropPoint;

    // 使用 C# 原生 Action，比 UnityEvent 更简洁高效
    public static event Action<string> OnAnyTreeShaken;

    private SpriteRenderer sr;
    private int applesLeft;
    private bool hasApples = true;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        applesLeft = UnityEngine.Random.Range(1, 4);
        if (dropPoint == null) dropPoint = transform.Find("Drop Point") ?? transform;
    }

    public void Interact()
    {
        if (!hasApples) return;

        DropApple();
        // 静态事件分发：谁感兴趣谁监听（比如 AI）
        OnAnyTreeShaken?.Invoke(gameObject.name);
    }

    void DropApple()
    {
        Instantiate(applePrefab, dropPoint.position, Quaternion.identity);
        if (--applesLeft <= 0)
        {
            hasApples = false;
            if (sr && emptyTreeSprite) sr.sprite = emptyTreeSprite;
        }
    }

    void OnMouseDown() => Interact();
}