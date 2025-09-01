using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class GPTSoVITSTextToSpeech : TTS
{
    #region 參數定義

    [Header("參考用的音訊（必填）")]
    [SerializeField] private AudioClip m_ReferenceClip = null;

    [Header("參考音訊對應的文字（必填）")]
    [SerializeField] private string m_ReferenceText = "";

    [Header("參考音訊的語言")]
    [SerializeField] private Language m_ReferenceTextLan = Language.Chinese;

    [Header("要合成語音的目標語言")]
    [SerializeField] private Language m_TargetTextLan = Language.Chinese;

    private string m_AudioBase64String = "";

    [SerializeField] private string m_SplitType = "不切";
    [SerializeField] private int m_Top_k = 5;
    [SerializeField] private float m_Top_p = 1f;
    [SerializeField] private float m_Temperature = 1f;
    [SerializeField] private bool m_TextReferenceMode = false;

    #endregion

    private void Awake()
    {
        AudioTurnToBase64();
    }

    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        StartCoroutine(GetVoice(_msg, _callback));
    }

    private IEnumerator GetVoice(string _msg, Action<AudioClip, string> _callback)
    {
        stopwatch.Restart();

        string _postJson = GetPostJson(_msg);

        using (UnityWebRequest request = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_postJson);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _text = request.downloadHandler.text;
                Response _response = JsonUtility.FromJson<Response>(_text);
                string _wavPath = _response.data[0].name;

                if (string.IsNullOrEmpty(_wavPath))
                {
                    StartCoroutine(GetVoice(_msg, _callback));
                }
                else
                {
                    StartCoroutine(GetAudioFromFile(_wavPath, _msg, _callback));
                }
            }
            else
            {
                Debug.LogError("語音合成失敗: " + request.error);
            }
        }

        stopwatch.Stop();
        Debug.Log("GPT-SoVITS 合成耗時: " + stopwatch.Elapsed.TotalSeconds);
    }

    private string GetPostJson(string _msg)
    {
        if (string.IsNullOrEmpty(m_ReferenceText) || m_ReferenceClip == null)
        {
            Debug.LogError("GPT-SoVITS 未設置參考音訊或文字");
            return null;
        }

        var jsonData = new
        {
            data = new List<object>
            {
                new { name = "audio.wav", data = "data:audio/wav;base64," + m_AudioBase64String },
                m_ReferenceText,
                m_ReferenceTextLan.ToString(),
                _msg,
                m_TargetTextLan.ToString(),
                m_SplitType,
                m_Top_k,
                m_Top_p,
                m_Temperature,
                m_TextReferenceMode
            }
        };

        return JsonConvert.SerializeObject(jsonData, Formatting.Indented);
    }

    private void AudioTurnToBase64()
    {
        if (m_ReferenceClip == null)
        {
            Debug.LogError("GPT-SoVITS 未設置參考音訊");
            return;
        }

        byte[] audioData = WavUtility.FromAudioClip(m_ReferenceClip);
        m_AudioBase64String = Convert.ToBase64String(audioData);
    }

    private IEnumerator GetAudioFromFile(string _path, string _msg, Action<AudioClip, string> _callback)
    {
        string filePath = "file://" + _path;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                _callback(audioClip, _msg);
            }
            else
            {
                Debug.LogError("讀取音訊失敗: " + request.error);
            }
        }
    }

    #region 回傳格式定義

    [Serializable]
    public class Response
    {
        public List<AudioBack> data = new List<AudioBack>();
        public bool is_generating = true;
        public float duration;
        public float average_duration;
    }

    [Serializable]
    public class AudioBack
    {
        public string name = string.Empty;
        public string data = string.Empty;
        public bool is_file = true;
    }

    public enum Language
    {
        Chinese,
        English,
        Japanese,
        ChineseEnglishMix,
        JapaneseEnglishMix,
        MultiLingual
    }

    #endregion
}
