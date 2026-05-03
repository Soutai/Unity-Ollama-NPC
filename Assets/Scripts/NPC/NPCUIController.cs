using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCUIController : MonoBehaviour
{
    [Header("UI 槽位 (请确保已拖入物体)")]
    public Slider hungerSlider;
    public TextMeshProUGUI thoughtText;
    public CanvasGroup canvasGroup;

    private NPCNeeds needs;
    private NPCUtilityBrain brain;

    void Start()
    {
        // 尝试获取父物体上的核心脚本
        needs = GetComponentInParent<NPCNeeds>();
        brain = GetComponentInParent<NPCUtilityBrain>();

        // 检查核心逻辑组件是否存在
        if (needs == null) Debug.LogError("NPCUIController: 找不到 NPCNeeds 组件，请检查层级！");
        if (brain == null) Debug.LogWarning("NPCUIController: 找不到 NPCUtilityBrain，无法显示内心独白。");

        if (hungerSlider != null)
        {
            hungerSlider.maxValue = 100f;
            hungerSlider.minValue = 0f;
        }
    }

    void Update()
    {
        // --- 核心防御：如果核心数据缺失，直接返回，不执行后面的逻辑 ---
        if (needs == null) return;

        // 1. 同步饥饿值 (只有在 Slider 被赋值了才执行)
        if (hungerSlider != null)
        {
            hungerSlider.value = needs.hunger;
            UpdateSliderColor();
        }

        // 2. 同步内心独白 (只有在 Text 和 Brain 都存在时才执行)
        if (thoughtText != null && brain != null)
        {
            thoughtText.text = brain.currentThought;
        }
    }

    void UpdateSliderColor()
    {
        if (hungerSlider.fillRect == null) return;

        Image fillImage = hungerSlider.fillRect.GetComponent<Image>();
        if (fillImage == null) return;

        if (needs.hunger < 25f) fillImage.color = Color.red;
        else if (needs.hunger < 60f) fillImage.color = Color.yellow;
        else fillImage.color = Color.green;
    }
}