using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class LLMClient : MonoBehaviour
{
    private string ollamaUrl = "http://localhost:11434/api/generate";

    // 这个方法用来给 AI 发送指令
    public void AskAI(string prompt, System.Action<string> callback)
    {
        StartCoroutine(PostRequest(prompt, callback));
    }

    IEnumerator PostRequest(string prompt, System.Action<string> callback)
    {
        // 构建 Ollama 请求数据
        var jsonData = new
        {
            model = "qwen2:1.5b",
            prompt = prompt,
            stream = false // 设为 false，一次性返回结果，方便新手处理
        };

        string jsonString = JsonUtility.ToJson(jsonData);
        // 注意：JsonUtility 对匿名对象支持不好，如果报错，建议手动拼字符串或使用 Newtonsoft.Json
        string manualJson = "{\"model\":\"qwen2:1.5b\",\"prompt\":\"" + prompt + "\",\"stream\":false}";

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(manualJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 解析返回的 JSON
                var response = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                callback?.Invoke(response.response);
            }
            else
            {
                Debug.LogError("AI 连接失败: " + request.error);
            }
        }
    }

    [System.Serializable]
    public class OllamaResponse { public string response; }
}