using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ChatLocalDeepSeek : LLM
{
    [Header("🧠 本地模型 API 設定")]
    [Tooltip("FastAPI 的 /chat 端點，例如：http://127.0.0.1:8000/generate")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:8000/generate";

    [Header("🔧 其他設定")]
    [SerializeField] private bool enableLog = true;

    // ✅ Request 函式：傳入使用者輸入文字，等待回傳模型回覆
    public override IEnumerator Request(string _postWord, Action<string> _callback)
    {
        // 傳給 API 的請求物件
        ChatRequest req = new ChatRequest { prompt = _postWord };

        string json = JsonConvert.SerializeObject(req);
        byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (enableLog)
        {
            Debug.Log($"📤 傳送至本地模型: {_postWord}");
        }

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                if (enableLog)
                    Debug.Log($"📥 收到回覆: {responseText}");

                try
                {
                    ChatResponse res = JsonConvert.DeserializeObject<ChatResponse>(responseText);
                    if (res != null && !string.IsNullOrEmpty(res.response))
                    {
                        _callback?.Invoke(res.response);
                    }
                    else
                    {
                        _callback?.Invoke("⚠️ 本地模型未回傳內容。");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ 回傳 JSON 解析錯誤: {ex.Message}");
                    _callback?.Invoke("⚠️ 回傳格式錯誤。");
                }
            }
            else
            {
                Debug.LogError($"❌ {request.responseCode} - {request.error}");
                _callback?.Invoke("⚠️ 無法連線至本地模型 API。");
            }
        }
    }

    // ✅ 傳入結構：與 FastAPI /chat 對應
    [Serializable]
    private class ChatRequest
    {
        public string prompt;
    }

    // ✅ 回傳結構：對應 {"response": "..."}
    [Serializable]
    private class ChatResponse
    {
        public string response;
    }
}
