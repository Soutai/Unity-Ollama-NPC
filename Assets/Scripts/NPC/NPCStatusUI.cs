using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCStatusUI : MonoBehaviour
{
    [Header("绑定 UI 组件")]
    public TextMeshProUGUI statusText;
    public Image hungerFillImage;
    public TextMeshProUGUI hungerPercentText; // 新增：显示百分比的文本

    [Header("颜色设置")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;

    [Header("追踪目标")]
    public NPCNeeds targetNPC;

    void Update()
    {
        if (targetNPC != null)
        {
            // 1. 更新饥饿条比例
            float fillAmount = targetNPC.hunger / 100f;
            hungerFillImage.fillAmount = fillAmount;

            // 2. 更新颜色逻辑 (60% 黄色, 30% 红色)
            UpdateBarColor(fillAmount);

            // 3. 更新百分比文字 (保留 0 位小数)
            hungerPercentText.text = (fillAmount * 100f).ToString("F0") + "%";

            // 4. 更新状态文本
            statusText.text = "NPC 行为: " + targetNPC.currentAction;
        }
    }

    void UpdateBarColor(float fill)
    {
        if (fill <= 0.3f) // 30% 以下
        {
            hungerFillImage.color = criticalColor;
        }
        else if (fill <= 0.6f) // 60% 以下
        {
            hungerFillImage.color = warningColor;
        }
        else
        {
            hungerFillImage.color = normalColor;
        }
    }
}