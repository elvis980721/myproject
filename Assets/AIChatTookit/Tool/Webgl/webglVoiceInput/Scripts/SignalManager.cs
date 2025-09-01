using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SignalManager : MonoBehaviour
{
    #region 公開方法

    /// <summary>
    /// 語音合成完成的回調
    /// </summary>
    public Action<AudioClip> onAudioClipDone;

    /// <summary>
    /// 開始錄音
    /// </summary>
    public void StartRecordBinding()
    {
        StartRecorderFunc();
    }

    /// <summary>
    /// 結束錄音
    /// </summary>
    public void StopRecordBinding()
    {
        EndRecorderFunc();
    }

    #endregion

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartRecord();

    [DllImport("__Internal")]
    private static extern void StopRecord();
#else
    private static void StartRecord()
    {
        Debug.Log("StartRecord called (stub for non-WebGL platform)");
    }

    private static void StopRecord()
    {
        Debug.Log("StopRecord called (stub for non-WebGL platform)");
    }
#endif

    void StartRecorderFunc()
    {
        StartRecord();
    }

    void EndRecorderFunc()
    {
        StopRecord();
    }

    #region JavaScript 呼叫 Unity
    #region 資料欄位

    /// <summary>
    /// 要接收的資料片段數量
    /// </summary>
    private int m_valuePartCount = 0;

    /// <summary>
    /// 已接收的資料片段數量
    /// </summary>
    private int m_getDataLength = 0;

    /// <summary>
    /// 接收到的音訊資料總長度
    /// </summary>
    private int m_audioLength = 0;

    /// <summary>
    /// 接收到的音訊資料片段
    /// </summary>
    private string[] m_audioData = null;

    /// <summary>
    /// 當前錄製的 AudioClip
    /// </summary>
    public static AudioClip m_audioClip = null;

    /// <summary>
    /// 音訊片段暫存列表
    /// </summary>
    private List<byte[]> m_audioClipDataList = new List<byte[]>();

    /// <summary>
    /// 當前錄音段落標記
    /// </summary>
    private string m_currentRecorderSign;

    /// <summary>
    /// 音訊頻率
    /// </summary>
    private int m_audioFrequency;

    /// <summary>
    /// 最大錄音時間（秒）
    /// </summary>
    private const int maxRecordTime = 30;

    #endregion

    /// <summary>
    /// 從 JavaScript 傳入音訊資料字串
    /// </summary>
    public void GetAudioData(string _audioDataString)
    {
        if (_audioDataString.Contains("Head"))
        {
            // 接收頭部資料
            string[] _headValue = _audioDataString.Split('|');
            m_valuePartCount = int.Parse(_headValue[1]);
            m_audioLength = int.Parse(_headValue[2]);
            m_currentRecorderSign = _headValue[3];
            m_audioData = new string[m_valuePartCount];
            m_getDataLength = 0;
            Debug.Log("接收到資料頭：片段數量 = " + m_valuePartCount + "，總長度 = " + m_audioLength);
        }
        else if (_audioDataString.Contains("Part"))
        {
            // 接收資料片段
            string[] _headValue = _audioDataString.Split('|');
            int _dataIndex = int.Parse(_headValue[1]);
            m_audioData[_dataIndex] = _headValue[2];
            m_getDataLength++;

            // 若片段都接收完成
            if (m_getDataLength == m_valuePartCount)
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < m_audioData.Length; i++)
                {
                    stringBuilder.Append(m_audioData[i]);
                }
                string _audioDataValue = stringBuilder.ToString();
                Debug.Log("接收資料長度：" + _audioDataValue.Length + " / 預期長度：" + m_audioLength);

                // 擷取最後的 Base64 音訊資料部分
                int _index = _audioDataValue.LastIndexOf(',');
                string _value = _audioDataValue.Substring(_index + 1);
                byte[] data = Convert.FromBase64String(_value);
                Debug.Log("已解碼資料長度：" + data.Length);

                if (m_currentRecorderSign == "end")
                {
                    // 合併所有音訊片段
                    int _audioLength = data.Length;
                    foreach (var part in m_audioClipDataList)
                    {
                        _audioLength += part.Length;
                    }

                    byte[] _audioData = new byte[_audioLength];
                    int _audioIndex = 0;
                    data.CopyTo(_audioData, _audioIndex);
                    _audioIndex += data.Length;

                    foreach (var part in m_audioClipDataList)
                    {
                        part.CopyTo(_audioData, _audioIndex);
                        _audioIndex += part.Length;
                    }

                    WAV wav = new WAV(_audioData);
                    AudioClip _audioClip = AudioClip.Create("TestWAV", wav.SampleCount, 1, wav.Frequency, false);
                    _audioClip.SetData(wav.LeftChannel, 0);

                    m_audioClip = _audioClip;
                    Debug.Log("音訊設置完成，長度：" + m_audioClip.length + "，名稱：" + m_audioClip.name);

                    onAudioClipDone?.Invoke(m_audioClip);
                    m_audioClipDataList.Clear();
                }
                else
                {
                    // 若還未結束，先儲存片段
                    m_audioClipDataList.Add(data);
                }

                m_audioData = null;
            }
        }
    }

    #endregion
}
