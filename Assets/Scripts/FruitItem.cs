using UnityEngine;

public class FruitItem : MonoBehaviour
{
    public float restoreAmount = 20f;
    public float interactDistance = 2f;
    private Transform playerTransform;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= interactDistance)
            {
                Eat();
            }
        }
    }

    void Eat()
    {
        // 只寻找饥饿系统脚本[cite: 4]
        HungerSystem hunger = playerTransform.GetComponent<HungerSystem>();
        if (hunger != null)
        {
            hunger.RestoreHunger(restoreAmount);
            Destroy(gameObject);
        }
    }
}