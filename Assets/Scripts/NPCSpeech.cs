using UnityEngine;
using TMPro;
using System.Collections;

public class NPCSpeech : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject speechBubble;
    public TextMeshProUGUI speechText;
    public float displayDuration = 4f;

    // 订阅全局树木摇晃事件
    void OnEnable() => TreeInteract.OnAnyTreeShaken += HandleTreeShaken;
    void OnDisable() => TreeInteract.OnAnyTreeShaken -= HandleTreeShaken;

    private void HandleTreeShaken(string treeName)
    {
        // 自动在场景中寻找现有的 LLMClient (本地 Ollama 客户端)
        LLMClient localLLM = Object.FindAnyObjectByType<LLMClient>();

        if (localLLM == null)
        {
            Debug.LogError("场景中找不到 LLMClient！请确保场景中有一个物体挂载了该脚本。");
            return;
        }

        StopAllCoroutines();

        // 构造发送给 Ollama 的指令
        // 这里加上 N3 级别的日语要求，让你的学习环境更真实
        string prompt = $"玩家摇晃了 {treeName}。请作为 NPC 用一句简短的中文吐槽这种行为，要求幽默且不超过 15 字。";

        // 精准对接你代码里的 AskAI 方法
        localLLM.AskAI(prompt, (response) => {
            StartCoroutine(ShowSpeech(response));
        });
    }

    private IEnumerator ShowSpeech(string message)
    {
        if (speechText == null || speechBubble == null) yield break;

        speechText.text = message;
        speechBubble.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        speechBubble.SetActive(false);
    }
}