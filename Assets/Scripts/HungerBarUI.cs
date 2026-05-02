using UnityEngine;
using UnityEngine.UI; // 必须引用 UI 命名空间

public class HungerBarUI : MonoBehaviour
{
    [Header("绑定组件")]
    public Image hungerFillImage;    // 拖入你的 HungerBar_Fill 图片
    private HungerSystem playerHunger; // 用于读取数据

    void Start()
    {
        // 自动找到玩家身上的饥饿系统
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHunger = player.GetComponent<HungerSystem>();
        }
    }

    void Update()
    {
        if (playerHunger != null && hungerFillImage != null)
        {
            // 计算填充比例：当前值 / 最大值
            float fillAmount = playerHunger.currentHunger / playerHunger.maxHunger;

            // 更新 Image 组件的 Fill Amount (确保 Image Type 设为 Filled)
            hungerFillImage.fillAmount = fillAmount;
        }
    }
}