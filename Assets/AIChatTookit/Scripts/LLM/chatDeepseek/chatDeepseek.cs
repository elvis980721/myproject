using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class ChatDeepSeek : LLM
{
    [Header("🔐 DeepSeek API 設定")]
    [Tooltip("API Key（從 https://deepseek.com 取得）")]
    [SerializeField] private string apiKey;

    [Tooltip("模型名稱，例如：deepseek-chat、deepseek-coder 等")]
    [SerializeField] private string modelName = "deepseek-chat";

    [Header("📝 其他設定")]
    [SerializeField] private bool enableLog = true;

    private string apiUrl = "https://api.deepseek.com/v1/chat/completions";

    // FAQ & DailyEvent 資料結構
    [Serializable]
    public class FAQ
    {
        public string question;
        public string answer;
    }

    [Serializable]
    public class SchoolEvent
    {
        public string 日期;
        public string 事件;
        public string 類別;
    }

    private List<FAQ> faqList = new List<FAQ>();
    private List<SchoolEvent> eventList = new List<SchoolEvent>();

    private IEnumerator Start()
    {
        // 先載入兩種知識庫
        yield return StartCoroutine(LoadFAQ("pccu_faq.json", faqList));
        yield return StartCoroutine(LoadEvents("daily_faq.json", eventList));

        InitPromptWithKnowledge(); // 初始化 Prompt
    }

    // 載入 FAQ JSON
    private IEnumerator LoadFAQ(string fileName, List<FAQ> targetList)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                targetList.AddRange(JsonConvert.DeserializeObject<List<FAQ>>(www.downloadHandler.text));
                if (enableLog) Debug.Log($"✅ 成功載入 {fileName}（Android），條目數量：{targetList.Count}");
            }
            else
            {
                Debug.LogWarning($"❗ {fileName} 載入失敗：" + www.error);
            }
        }
#else
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            targetList.AddRange(JsonConvert.DeserializeObject<List<FAQ>>(jsonText));
            if (enableLog) Debug.Log($"✅ 成功載入 {fileName}（PC），條目數量：{targetList.Count}");
        }
        else
        {
            Debug.LogWarning($"❗ {fileName} 檔案不存在：" + path);
        }
        yield return null;
#endif
    }

    // 載入每日事件 JSON
    private IEnumerator LoadEvents(string fileName, List<SchoolEvent> targetList)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                targetList.AddRange(JsonConvert.DeserializeObject<List<SchoolEvent>>(www.downloadHandler.text));
                if (enableLog) Debug.Log($"✅ 成功載入 {fileName}（Android），條目數量：{targetList.Count}");
            }
            else
            {
                Debug.LogWarning($"❗ {fileName} 載入失敗：" + www.error);
            }
        }
#else
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            targetList.AddRange(JsonConvert.DeserializeObject<List<SchoolEvent>>(jsonText));
            if (enableLog) Debug.Log($"✅ 成功載入 {fileName}（PC），條目數量：{targetList.Count}");
        }
        else
        {
            Debug.LogWarning($"❗ {fileName} 檔案不存在：" + path);
        }
        yield return null;
#endif
    }

    // 建立 Prompt（FAQ + Daily Events）
    private void InitPromptWithKnowledge()
    {
        m_DataList = new List<SendData>();

        string knowledge = "你是活潑又有點中二的少女胡桃，現在正在幫助使用者解答有關文化大學與校園活動的常見問題，請以親切、有趣但專業的口吻回答。\n\n";

        // 加入 FAQ
        foreach (var faq in faqList)
        {
            knowledge += $"Q: {faq.question}\nA: {faq.answer}\n\n";
        }

        // 加入每日事件
        foreach (var ev in eventList)
        {
            knowledge += $"日期: {ev.日期}  事件: {ev.事件}  類別: {ev.類別}\n";
        }

        m_DataList.Add(new SendData("system", knowledge));
    }

    // DeepSeek 請求
    public override IEnumerator Request(string _postWord, Action<string> _callback)
    {
        m_DataList.Add(new SendData("user", _postWord));

        PostData postData = new PostData
        {
            model = modelName,
            messages = m_DataList,
            stream = false
        };

        string json = JsonConvert.SerializeObject(postData);
        byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(postBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        if (enableLog)
        {
            Debug.Log("📤 正在傳送請求至 DeepSeek API...");
            Debug.Log($"📬 傳送內容：{json}");
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            MessageBack messageBack = JsonConvert.DeserializeObject<MessageBack>(responseText);

            if (messageBack != null && messageBack.choices != null && messageBack.choices.Count > 0)
            {
                string aiReply = messageBack.choices[0].message.content;
                m_DataList.Add(new SendData("assistant", aiReply));
                _callback?.Invoke(aiReply);
            }
            else
            {
                _callback?.Invoke("⚠️ 沒有獲得有效的回覆。");
            }
        }
        else
        {
            Debug.LogError($"❌ {request.responseCode} - {request.error}");
            _callback?.Invoke("⚠️ 發送失敗，請檢查網路與 API Key。");
        }
    }

    // DeepSeek API 結構
    [Serializable]
    private class PostData
    {
        public string model;
        public List<SendData> messages;
        public bool stream;
    }

    [Serializable]
    private class MessageBack
    {
        public string id;
        public string created;
        public string model;
        public List<Choice> choices;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
        public string finish_reason;
        public int index;
    }

    [Serializable]
    private class Message
    {
        public string role;
        public string content;
    }
}
