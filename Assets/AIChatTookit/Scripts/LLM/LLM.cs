using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class LLM : MonoBehaviour
{
   
    [SerializeField] protected string url;

    
    [Header("發送的提示詞設定")]
    [SerializeField] protected string m_Prompt = string.Empty;

    [Header("設定回覆的語言")]
    [SerializeField] protected string lan = "中文";

    [Header("上下文保留條數")]
    [SerializeField] protected int m_HistoryKeepCount = 15;

    [SerializeField] public List<SendData> m_DataList = new List<SendData>();

    [NonSerialized]  
    protected Stopwatch stopwatch = new Stopwatch();

    public virtual void PostMsg(string _msg, Action<string> _callback)
    {
        CheckHistory();

        string message = "當前為角色的人物設定：" + m_Prompt +
                         " 回答的語言：" + lan +
                         " 接下來是我的提問：" + _msg;

        m_DataList.Add(new SendData("user", message));

        StartCoroutine(Request(message, _callback));
    }

    public virtual IEnumerator Request(string _postWord, Action<string> _callback)
    {
        yield return new WaitForEndOfFrame();
        // 子類別請實作自己的邏輯
    }

    public virtual void CheckHistory()
    {
        if (m_DataList.Count > m_HistoryKeepCount)
        {
            m_DataList.RemoveAt(0);
        }
    }

    [Serializable]
    public class SendData
    {
        [SerializeField] public string role;
        [SerializeField] public string content;

        public SendData() { }

        public SendData(string _role, string _content)
        {
            role = _role;
            content = _content;
        }
    }
}
