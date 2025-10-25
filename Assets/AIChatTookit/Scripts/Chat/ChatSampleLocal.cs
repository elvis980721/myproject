using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using WebGLSupport;

public class ChatSampleLocal : MonoBehaviour
{
    [Header("🧠 本地模型 API 設定")]
    [SerializeField] private string m_PostURL = "http://127.0.0.1:8000/generate";

    #region UI 定義
    [SerializeField] private GameObject m_ChatPanel;
    [SerializeField] public InputField m_InputWord;
    [SerializeField] private Text m_TextBack;
    [SerializeField] private Text m_AnswerText;
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField] private Button m_CommitMsgBtn;
    #endregion

    #region 聊天與語音設定
    [Header("設定是否透過語音合成播放文字")]
    [SerializeField] private bool m_IsVoiceMode = true;

    [Header("勾選則不發送給 LLM，直接合成輸入文字")]
    [SerializeField] private bool m_CreateVoiceMode = false;

    [Header("情緒模式 (影響語音播放)")]
    [SerializeField] private Emotion m_EmotionMode = Emotion.Neutral;

    [Header("TTS 系統 (可選)")]
    [SerializeField] private TTS m_TextToSpeech;

    private List<string> m_ChatHistory = new List<string>();
    #endregion

    #region 打字效果
    [SerializeField] private float m_WordWaitTime = 0.2f;
    [SerializeField] private bool m_WriteState = false;
    [SerializeField] private int m_MaxVisibleLines = 3;
    private Coroutine typingCoroutine;
    #endregion

    #region 關鍵字圖片顯示
    [SerializeField] private Image m_KeywordImage;

    private Dictionary<string, string> m_KeywordImageMap = new Dictionary<string, string>()
    {
        { "大恩館", "大恩館" },
        { "大典館", "大典館" },
        { "大成館", "大成館" },
        { "大義館", "大義館" },
        { "大孝館", "大孝館" },
        { "大倫館", "大倫館" },
        { "大慈館", "大慈館" },
        { "大莊館", "大莊館" },
        { "大賢館", "大賢館" },
        { "大雅館", "大雅館" },
        { "曉峰紀念館", "曉峰紀念館" }
    };
    private Dictionary<string, Sprite> m_KeywordSpriteMap = new Dictionary<string, Sprite>();
    private Coroutine imageCoroutine;
    #endregion

    private void Awake()
    {
        m_CommitMsgBtn.onClick.AddListener(delegate { SendData(); });
        InputSettingWhenWebgl();

        // ✅ 載入圖片資源
        foreach (var pair in m_KeywordImageMap)
        {
            Sprite sprite = Resources.Load<Sprite>("Images/" + pair.Value);
            if (sprite != null)
                m_KeywordSpriteMap[pair.Key] = sprite;
        }

        if (m_KeywordImage != null)
            m_KeywordImage.gameObject.SetActive(false);
    }

    private void InputSettingWhenWebgl()
    {
#if UNITY_WEBGL
        m_InputWord.gameObject.AddComponent<WebGLSupport.WebGLInput>();
#endif
    }

    #region 發送訊息邏輯
    public void SendData()
    {
        SendData(m_InputWord.text);
    }

    public void SendData(string _postWord)
    {
        if (string.IsNullOrEmpty(_postWord)) return;

        if (m_CreateVoiceMode)
        {
            if (m_IsVoiceMode && m_TextToSpeech != null)
                m_TextToSpeech.Speak(_postWord, PlayVoice);

            ShowKeywordImage(_postWord);
            StartTypeWords(_postWord);
            m_InputWord.text = "";
            return;
        }

        // ✅ 清空前次回答，避免「正在思考中」疊字
        m_AnswerText.text = "";
        m_TextBack.text = "正在思考中...";
        m_InputWord.text = "";

        // ✅ 呼叫本地 FastAPI
        m_ChatHistory.Add(_postWord);
        StartCoroutine(SendToLocalModel(_postWord));
    }

    private IEnumerator SendToLocalModel(string userText)
    {
        var req = new ChatRequest { prompt = userText };
        string json = JsonConvert.SerializeObject(req);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(m_PostURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 連線錯誤: {request.error}");
                m_TextBack.text = "⚠️ 伺服器未回應";
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"📥 API 回傳: {responseText}");

                ChatResponse res = JsonConvert.DeserializeObject<ChatResponse>(responseText);
                if (res != null && !string.IsNullOrEmpty(res.response))
                    CallBack(res.response);
                else
                    m_TextBack.text = "⚠️ 模型未產生有效回覆";
            }
        }
    }
    #endregion

    #region 顯示圖片邏輯
    private void ShowKeywordImage(string responseText)
    {
        List<(int index, Sprite sprite)> matches = new List<(int, Sprite)>();

        foreach (var pair in m_KeywordSpriteMap)
        {
            int pos = responseText.IndexOf(pair.Key);
            if (pos >= 0) matches.Add((pos, pair.Value));
        }

        if (matches.Count == 0)
        {
            m_KeywordImage.gameObject.SetActive(false);
            return;
        }

        matches.Sort((a, b) => a.index.CompareTo(b.index));
        if (imageCoroutine != null)
            StopCoroutine(imageCoroutine);

        imageCoroutine = StartCoroutine(ShowImagesInOrder(matches));
    }

    private IEnumerator ShowImagesInOrder(List<(int index, Sprite sprite)> matches)
    {
        m_KeywordImage.gameObject.SetActive(true);

        foreach (var match in matches)
        {
            m_KeywordImage.sprite = match.sprite;
            m_KeywordImage.canvasRenderer.SetAlpha(0f);
            m_KeywordImage.CrossFadeAlpha(1f, 0.5f, false);
            yield return new WaitForSeconds(2f);
            m_KeywordImage.CrossFadeAlpha(0f, 0.5f, false);
            yield return new WaitForSeconds(0.5f);
        }

        m_KeywordImage.gameObject.SetActive(false);
        imageCoroutine = null;
    }
    #endregion

    #region 回覆與語音
    private void CallBack(string _response)
    {
        _response = _response.Trim();
        m_TextBack.text = ""; // ✅ 清除「正在思考中」
        m_AnswerText.text = ""; // ✅ 確保清空舊字
        m_ChatHistory.Add(_response);

        ShowKeywordImage(_response);
        StartTypeWords(_response);

        if (m_IsVoiceMode && m_TextToSpeech != null)
            m_TextToSpeech.Speak(_response, PlayVoice);
    }

    private void PlayVoice(AudioClip clip, string text)
    {
        if (clip == null) return;
        m_AudioSource.clip = clip;

        switch (m_EmotionMode)
        {
            case Emotion.Happy:
                m_AudioSource.pitch = 1.2f;
                m_AudioSource.volume = 1.0f;
                break;
            case Emotion.Angry:
                m_AudioSource.pitch = 1.1f;
                m_AudioSource.volume = 1.3f;
                break;
            case Emotion.Sad:
                m_AudioSource.pitch = 0.85f;
                m_AudioSource.volume = 0.8f;
                break;
            default:
                m_AudioSource.pitch = 1.0f;
                m_AudioSource.volume = 1.0f;
                break;
        }

        m_AudioSource.Play();
    }
    #endregion

    #region 打字動畫
    private void StartTypeWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        m_AnswerText.text = ""; // ✅ 打字前清空文字
        typingCoroutine = StartCoroutine(SetTextPerWord(text));
    }

    private IEnumerator SetTextPerWord(string text)
    {
        m_WriteState = true;
        int currentPos = 0;

        while (currentPos < text.Length)
        {
            m_AnswerText.text = text.Substring(0, currentPos + 1);
            LimitVisibleLines();
            currentPos++;
            yield return new WaitForSeconds(m_WordWaitTime);
        }

        m_WriteState = false;
        typingCoroutine = null;
    }

    private void LimitVisibleLines()
    {
        string[] lines = m_AnswerText.text.Split('\n');
        if (lines.Length > m_MaxVisibleLines)
        {
            int start = lines.Length - m_MaxVisibleLines;
            m_AnswerText.text = string.Join("\n", lines, start, m_MaxVisibleLines);
        }
    }
    #endregion

    #region JSON 結構
    [Serializable]
    private class ChatRequest
    {
        public string prompt;
    }

    [Serializable]
    private class ChatResponse
    {
        public string response;
    }
    #endregion
}

public enum Emotion
{
    Neutral,
    Happy,
    Angry,
    Sad
}
