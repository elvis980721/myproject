using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static GPTSoVITSTextToSpeech;

public class GPTSoVITSFASTAPI : TTS
{
    [Header("參考音訊路徑 (相對 GPT-SoVITS 根目錄)")]
    [SerializeField] private string m_ReferWavPath = "audio/hutao.wav";

    [Header("參考語音文字")]
    [SerializeField] private string m_ReferenceText = "我是景元";

    [Header("參考語言")]
    [SerializeField] private Language m_ReferenceTextLan = Language.Chinese;

    [Header("目標語言")]
    [SerializeField] private Language m_TargetTextLan = Language.Chinese;



    // 預設取樣率（需根據實際音訊設定）
    private const int SAMPLE_RATE = 32000;

    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        Debug.Log($"🟨 Speak 執行，訊息：{_msg}");
        StartCoroutine(GetVoice(_msg, _callback));
    }

    /// <summary>
    /// Sends a request to convert text to speech
    /// </summary>
    /// <param name="_msg">The text to convert</param>
    /// <param name="_callback">Callback to handle the generated audio clip</param>
    /// <returns>IEnumerator for coroutine</returns>
    private IEnumerator GetVoice(string _msg, Action<AudioClip, string> _callback)
    {
        Debug.Log("🟨 GetVoice() 開始");

        stopwatch.Restart();
        // Prepare request data
        RequestData requestData = new RequestData
        {
            refer_wav_path = m_ReferWavPath,
            prompt_text = m_ReferenceText,
            prompt_language = m_ReferenceTextLan.ToApiString(), // 使用 ToApiString
            text = _msg,
            text_language = m_TargetTextLan.ToApiString() // 使用 ToApiString

        };

        string json = JsonConvert.SerializeObject(requestData);
        Debug.Log($"📨 要送出的 JSON：{json}");
        Debug.Log($"📨 API URL：{m_PostURL}");


        using (UnityWebRequest request = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer(); // 接收 byte[]
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                byte[] audioBytes = request.downloadHandler.data;
                Debug.Log($"✅ 音訊接收成功，大小: {audioBytes.Length} bytes");

                AudioClip clip = WavUtility.ConvertBytesToAudioClip(audioBytes, SAMPLE_RATE);
                Debug.Log($"📦 轉換音檔成功？{(clip != null)}，長度: {clip?.length}s");
                if (clip != null)
                {
                    _callback(clip, _msg);
                }
                else
                {
                    Debug.LogError("⚠️ 音訊轉換失敗，可能不是有效的 WAV 檔。");
                    _callback(null, _msg);
                }
            }
            else
            {
                Debug.LogError($"❌ 語音合成失敗: {request.error}\n回應內容: {request.downloadHandler.text}");
                _callback(null, _msg);
            }
        }
    }


    [Serializable]
    public class RequestData
    {
        public string refer_wav_path = string.Empty; // Path to reference audio file
        public string prompt_text = string.Empty; // Reference text content
        public string prompt_language = string.Empty; // Language of reference text
        public string text = string.Empty; // Target text to convert
        public string text_language = string.Empty; // Language of target text
    }

    public enum Language
    {
        Chinese,
        Japanese,
        English
    }
}

public static class LanguageExtensions
{
    public static string ToApiString(this GPTSoVITSFASTAPI.Language lang)
    {
        switch (lang)
        {
            case GPTSoVITSFASTAPI.Language.Chinese: return "zh";
            case GPTSoVITSFASTAPI.Language.Japanese: return "ja";
            case GPTSoVITSFASTAPI.Language.English: return "en";
            default: return "zh"; // 預設中文
        }
    }

}