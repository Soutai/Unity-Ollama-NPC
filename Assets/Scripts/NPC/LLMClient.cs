using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class LLMClient : MonoBehaviour
{
    private string ollamaUrl = "http://localhost:11434/api/generate";

    [Header("Streaming 设置")]
    public float charactersPerSecond = 30f; // 控制打字速度

    /// <summary>
    /// 普通一次性请求（保持原有接口）
    /// </summary>
    public void AskAI(string prompt, System.Action<string> callback)
    {
        StartCoroutine(PostRequest(prompt, callback));
    }

    /// <summary>
    /// Streaming 版本（推荐用于 NPC 对话）
    /// </summary>
    public void AskAIStream(string prompt, Action<string> onTokenReceived, Action<string> onComplete = null)
    {
        StartCoroutine(StreamRequest(prompt, onTokenReceived, onComplete));
    }

    // ==================== 一次性请求 ====================
    IEnumerator PostRequest(string prompt, System.Action<string> callback)
    {
        string json = $"{{\"model\":\"qwen2:1.5b\",\"prompt\":\"{EscapeJson(prompt)}\",\"stream\":false}}";

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                callback?.Invoke(response.response);
            }
            else
            {
                Debug.LogError("Ollama 请求失败: " + request.error);
                callback?.Invoke("...（连接失败）");
            }
        }
    }

    // ==================== Streaming 请求 ====================
    IEnumerator StreamRequest(string prompt, Action<string> onTokenReceived, Action<string> onComplete)
    {
        string json = $"{{\"model\":\"qwen2:1.5b\",\"prompt\":\"{EscapeJson(prompt)}\",\"stream\":true}}";

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Streaming 请求失败: " + request.error);
                onComplete?.Invoke("...（连接失败）");
                yield break;
            }

            string fullResponse = "";
            string[] lines = request.downloadHandler.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var chunk = JsonUtility.FromJson<OllamaStreamChunk>(line);
                    if (!string.IsNullOrEmpty(chunk.response))
                    {
                        fullResponse += chunk.response;
                        onTokenReceived?.Invoke(chunk.response); // 逐 token 返回
                    }

                    if (chunk.done)
                    {
                        onComplete?.Invoke(fullResponse);
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("解析 Streaming JSON 失败: " + e.Message);
                }

                // 控制输出速度（可选）
                if (charactersPerSecond > 0)
                    yield return new WaitForSeconds(1f / charactersPerSecond);
            }
        }
    }

    // 简单转义 JSON 特殊字符
    private string EscapeJson(string str)
    {
        return str.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    [System.Serializable]
    public class OllamaResponse { public string response; }

    [System.Serializable]
    public class OllamaStreamChunk
    {
        public string response;
        public bool done;
    }
}