using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceInputs : MonoBehaviour
{
    /// <summary>
    /// 錄音長度（秒）
    /// </summary>
    public int m_RecordingLength = 5;

    public AudioClip recording;

    /// <summary>
    /// WebGL 支援類別
    /// </summary>
    [SerializeField] private SignalManager signalManager;

    /// <summary>
    /// 開始錄音
    /// </summary>
    public void StartRecordAudio()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        signalManager.onAudioClipDone = null;
        signalManager.StartRecordBinding();
#else
        recording = null;
        recording = Microphone.Start(null, false, m_RecordingLength, 16000);
#endif
    }

    /// <summary>
    /// 停止錄音，回傳 AudioClip
    /// </summary>
    /// <param name="_callback">錄音完成後的回呼函數</param>
    public void StopRecordAudio(Action<AudioClip> _callback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        signalManager.onAudioClipDone += _callback;
        signalManager.StopRecordBinding();
#else
        Microphone.End(null);
        _callback(recording);
#endif
    }
}
