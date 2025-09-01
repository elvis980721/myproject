using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WebGLSupport;

public class ChatSample : MonoBehaviour
{
    [SerializeField] private ChatSetting m_ChatSettings;

    #region UI 定義
    [SerializeField] private GameObject m_ChatPanel;
    [SerializeField] public InputField m_InputWord;
    [SerializeField] private Text m_TextBack;
    [SerializeField] private Text m_AnswerText;
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField] private Button m_CommitMsgBtn;
    #endregion

    #region 參數定義
    [SerializeField] private Animator m_Animator;

    [Header("設定是否透過語音合成播放文字")]
    [SerializeField] private bool m_IsVoiceMode = true;

    [Header("勾選則不發送給 LLM，直接合成輸入文字")]
    [SerializeField] private bool m_CreateVoiceMode = false;

    [Header("情緒模式 (影響語音播放)")]
    [SerializeField] private Emotion m_EmotionMode = Emotion.Neutral;

    private List<string> m_ChatHistory = new List<string>();
    #endregion

    #region 聊天記錄顯示
    [SerializeField] private List<GameObject> m_TempChatBox;
    [SerializeField] private GameObject m_HistoryPanel;
    [SerializeField] private RectTransform m_rootTrans;
    [SerializeField] private ChatPrefab m_PostChatPrefab;
    [SerializeField] private ChatPrefab m_RobotChatPrefab;
    [SerializeField] private ScrollRect m_ScroTectObject;
    #endregion

    #region 打字機效果
    [SerializeField] private float m_WordWaitTime = 0.2f;
    [SerializeField] private bool m_WriteState = false;
    #endregion

    #region 輸出圖片
    [SerializeField] private Image m_KeywordImage;

    // 關鍵字對應圖片檔名
    private Dictionary<string, string> m_KeywordImageMap = new Dictionary<string, string>()
    {
        { "大恩館", "大恩館" },
        { "大典館", "大典館" },
        { "大功館", "大功館" },
        { "大義館", "大義館" },
        { "大孝館", "大孝館" },
        { "大倫館", "大倫館" },
        { "大慈館", "大慈館" },
        { "大莊館", "大莊館" },
        { "大賢館", "大賢館" },
        { "大雅館", "大雅館" },
        { "曉峰紀念館", "曉峰紀念館" }
    };

    // 快取好的圖片
    private Dictionary<string, Sprite> m_KeywordSpriteMap = new Dictionary<string, Sprite>();
    #endregion

    private void Awake()
    {
        m_CommitMsgBtn.onClick.AddListener(delegate { SendData(); });
        InputSettingWhenWebgl();

        // ✅ 在 Awake 時先載入圖片
        foreach (var pair in m_KeywordImageMap)
        {
            Sprite sprite = Resources.Load<Sprite>("Images/" + pair.Value);
            if (sprite != null)
            {
                m_KeywordSpriteMap[pair.Key] = sprite;
            }
            else
            {
                Debug.LogWarning($"⚠️ 找不到圖片：Images/{pair.Value}.jpg");
            }
        }

        if (m_KeywordImage != null)
            m_KeywordImage.gameObject.SetActive(false);
    }

    private Coroutine imageCoroutine;
    private void ShowKeywordImage(string responseText)
    {
        // 找出所有符合的圖片 + 出現順序
        List<(int index, Sprite sprite)> matches = new List<(int, Sprite)>();

        foreach (var pair in m_KeywordSpriteMap)
        {
            int pos = responseText.IndexOf(pair.Key);
            if (pos >= 0) // 找到了關鍵字
            {
                matches.Add((pos, pair.Value));
            }
        }

        // 沒有符合 → 隱藏圖片
        if (matches.Count == 0)
        {
            m_KeywordImage.gameObject.SetActive(false);
            return;
        }

        // 按照文字中出現的位置排序
        matches.Sort((a, b) => a.index.CompareTo(b.index));

        // 如果已有輪播在跑 → 停掉
        if (imageCoroutine != null)
            StopCoroutine(imageCoroutine);

        // 啟動輪播
        imageCoroutine = StartCoroutine(ShowImagesInOrder(matches));
    }

    private IEnumerator ShowImagesInOrder(List<(int index, Sprite sprite)> matches)
    {
        m_KeywordImage.gameObject.SetActive(true);

        foreach (var match in matches)
        {
            // 換圖 + 淡入
            m_KeywordImage.sprite = match.sprite;
            m_KeywordImage.canvasRenderer.SetAlpha(0f);
            m_KeywordImage.CrossFadeAlpha(1f, 0.5f, false);

            // 顯示 2 秒（可調整）
            yield return new WaitForSeconds(2f);

            // 淡出
            m_KeywordImage.CrossFadeAlpha(0f, 0.5f, false);
            yield return new WaitForSeconds(0.5f);
        }

        // 最後隱藏圖片
        m_KeywordImage.gameObject.SetActive(false);
        imageCoroutine = null;
    }


    private void InputSettingWhenWebgl()
    {
#if UNITY_WEBGL
        m_InputWord.gameObject.AddComponent<WebGLSupport.WebGLInput>();
#endif
    }

    #region 發送訊息
    public void SendData()
    {
        SendData(m_InputWord.text);
    }

    public void SendData(string _postWord)
    {
        if (string.IsNullOrEmpty(_postWord)) return;

        // 修改：勾選 m_CreateVoiceMode -> 直接合成輸入文字
        if (m_CreateVoiceMode)
        {
            if (m_IsVoiceMode && m_ChatSettings.m_TextToSpeech != null)
            {
                m_ChatSettings.m_TextToSpeech.Speak(_postWord, PlayVoice);
            }

            ShowKeywordImage(_postWord); // 🔑 直接輸入文字也觸發圖片顯示
            StartTypeWords(_postWord);   // 顯示文字

            m_InputWord.text = "";
            return;
        }

        // 原本流程：發送給 LLM
        m_ChatHistory.Add(_postWord);
        m_ChatSettings.m_ChatModel.PostMsg(_postWord, CallBack);

        m_InputWord.text = "";
        m_TextBack.text = "正在思考中...";
        SetAnimator("state", 1);
    }
    #endregion

    #region 播放語音 + 情緒控制
    private void PlayVoice(AudioClip clip, string text)
    {
        if (clip == null) return;
        m_AudioSource.clip = clip;

        // 🎭 根據情緒模式調整音效
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
            default: // Neutral
                m_AudioSource.pitch = 1.0f;
                m_AudioSource.volume = 1.0f;
                break;
        }

        m_AudioSource.Play();
    }
    #endregion

    #region 處理回應
    private void CallBack(string _response)
    {
        _response = _response.Trim();
        m_TextBack.text = ""; // 清除「正在思考中」
        m_AnswerText.text = ""; // 清除前次回答文字
        m_ChatHistory.Add(_response);

        ShowKeywordImage(_response); // 🔑 加這行，確保顯示圖片

        StartTypeWords(_response); // 顯示文字

        if (m_IsVoiceMode && m_ChatSettings.m_TextToSpeech != null)
        {
            m_ChatSettings.m_TextToSpeech.Speak(_response, PlayVoice);
        }
    }
    #endregion

    #region 打字動畫

    [SerializeField] private int m_MaxVisibleLines = 3; // ✅ 限制為 3 行

    private Coroutine typingCoroutine;

    private void StartTypeWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(SetTextPerWord(text));
    }

    private IEnumerator SetTextPerWord(string text)
    {
        m_WriteState = true;
        m_AnswerText.text = "";

        int currentPos = 0;

        while (currentPos < text.Length)
        {
            m_AnswerText.text = text.Substring(0, currentPos + 1);

            // ✅ 行數限制檢查
            LimitVisibleLines();

            currentPos++;
            yield return new WaitForSeconds(m_WordWaitTime);
        }

        m_WriteState = false;
        typingCoroutine = null;

        // ✅ 動畫狀態還原（打字完成後）
        SetAnimator("state", 0);
    }

    /// <summary>
    /// 限制對話框文字的行數
    /// </summary>
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

    #region 聊天歷史
    public void OpenAndGetHistory()
    {
        m_ChatPanel.SetActive(false);
        m_HistoryPanel.SetActive(true);
        ClearChatBox();
        StartCoroutine(GetHistoryChatInfo());
    }

    public void BackChatMode()
    {
        m_ChatPanel.SetActive(true);
        m_HistoryPanel.SetActive(false);
    }

    private void ClearChatBox()
    {
        foreach (GameObject obj in m_TempChatBox)
        {
            if (obj != null) Destroy(obj);
        }
        m_TempChatBox.Clear();
    }

    private IEnumerator GetHistoryChatInfo()
    {
        yield return new WaitForEndOfFrame();

        for (int i = 0; i < m_ChatHistory.Count; i++)
        {
            ChatPrefab chat = Instantiate(i % 2 == 0 ? m_PostChatPrefab : m_RobotChatPrefab, m_rootTrans);
            chat.SetText(m_ChatHistory[i]);
            m_TempChatBox.Add(chat.gameObject);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(m_rootTrans);
        StartCoroutine(TurnToLastLine());
    }

    private IEnumerator TurnToLastLine()
    {
        yield return new WaitForEndOfFrame();
        m_ScroTectObject.verticalNormalizedPosition = 0;
    }
    #endregion

    private void SetAnimator(string key, int value)
    {
        if (m_Animator != null)
        {
            m_Animator.SetInteger(key, value);
        }
    }
}
public enum Emotion
{
    Neutral,
    Happy,
    Angry,
    Sad
}
