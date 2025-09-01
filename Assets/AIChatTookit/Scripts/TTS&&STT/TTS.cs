using System;
using System.Diagnostics;
using UnityEngine;

public class TTS : MonoBehaviour
{
    /// <summary>
    /// 要呼叫的 API 地址
    /// </summary>
    [SerializeField] protected string m_PostURL = string.Empty;

    /// <summary>
    /// 計算方法調用時間
    /// </summary>
    [NonSerialized] protected Stopwatch stopwatch = new Stopwatch();

    /// <summary>
    /// 語音合成，回傳 AudioClip
    /// </summary>
    public virtual void Speak(string _msg, Action<AudioClip> _callback) { }

    /// <summary>
    /// 語音合成，回傳 AudioClip 與原始訊息
    /// </summary>
    public virtual void Speak(string _msg, Action<AudioClip, string> _callback) { }
}