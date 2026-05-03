using UnityEngine;
using TMPro;
using System.Collections;

public class NPCSpeech : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject speechBubble;
    public TextMeshProUGUI speechText;

    [Header("显示设置")]
    [Tooltip("文字完全显示后停留的时间（秒）")]
    public float displayDuration = 8f;

    private Coroutine autoHideCoroutine;

    // 订阅事件
    void OnEnable() => TreeInteract.OnAnyTreeShaken += HandleTreeShaken;
    void OnDisable() => TreeInteract.OnAnyTreeShaken -= HandleTreeShaken;

    private void HandleTreeShaken(string treeName)
    {
        // 查找场景中的 LLMClient
        LLMClient localLLM = FindObjectOfType<LLMClient>();

        if (localLLM == null)
        {
            Debug.LogError("场景中找不到 LLMClient！请检查场景物体。");
            return;
        }

        // 停止之前的倒计时
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);

        // 重置 UI 状态
        speechText.text = "";
        if (speechBubble != null)
            speechBubble.SetActive(true);

        // 构造 AI 提示词
        string prompt = $"玩家摇晃了 {treeName}。请作为 NPC 用一句简短的中文吐槽这种行为，要求幽默且不超过 15 字。";

        // 发起流式请求
        localLLM.AskAIStream(
            prompt,
            onTokenReceived: (token) =>
            {
                // 直接累加字符
                speechText.text += token;
            },
            onComplete: (fullText) =>
            {
                // 确保显示完整文本
                speechText.text = fullText;

                // 播放完毕后，启动自动消失倒计时
                if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = StartCoroutine(AutoHideSpeech());
            }
        );
    }

    private IEnumerator AutoHideSpeech()
    {
        // 等待设定的时长
        yield return new WaitForSeconds(displayDuration);

        if (speechBubble != null)
            speechBubble.SetActive(false);

        speechText.text = "";
    }

    public void HideSpeech()
    {
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);

        if (speechBubble != null)
            speechBubble.SetActive(false);

        speechText.text = "";
    }
}