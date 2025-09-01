using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ChatZhipu : LLM
{

    #region ����
    /// <summary>
    /// ѡ���ģ��
    /// </summary>
    [SerializeField] public string m_ChatModelName = "glm-4-flash-250414";
    /// <summary>
    /// ���÷�ʽ  invoke/async-invoke/sse-invoke  ��ʵ��ͬ��ģʽ
    /// </summary>
    [SerializeField] private string m_InvokeMethod = "invoke";
    /// <summary>
    /// AI�趨
    /// </summary>
    public string m_SystemSetting = string.Empty;
    /// <summary>
    /// ����AI��apikey
    /// </summary>
    [Header("��д����AI��apikey")]
    [SerializeField] private string m_Key = string.Empty;
    //api key
    private string m_ApiKey = string.Empty;
    //secret key
    private string m_SecretKey = string.Empty;
    #endregion

    private void Awake()
    {
        OnInit();
    }
    /// <summary>
    /// ��ʼ��
    /// </summary>
    private void OnInit()
    {
        //����ʱ�����AI�趨
        m_DataList.Add(new SendData("system", m_SystemSetting));
        url = "https://open.bigmodel.cn/api/paas/v4/chat/completions";
        SplitKey();
    }

    /// <summary>
    /// ������Ϣ
    /// </summary>
    /// <returns></returns>
    public override void PostMsg(string _msg, Action<string> _callback)
    {
        base.PostMsg(_msg, _callback);
    }


    /// <summary>
    /// ��������
    /// </summary> 
    /// <param name="_postWord"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public override IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        stopwatch.Start();
        string jsonPayload = JsonConvert.SerializeObject(new PostData
        {
            model = m_ChatModelName,
            messages = m_DataList
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", GetToken());

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msgBack = request.downloadHandler.text;
                MessageBack _textback = JsonUtility.FromJson<MessageBack>(_msgBack);
                if (_textback != null && _textback.choices.Count > 0)
                {

                    string _backMsg = _textback.choices[0].message.content;
                    //��Ӽ�¼
                    m_DataList.Add(new SendData("assistant", _backMsg));
                    _callback(_backMsg);
                }
            }
            else
            {
                string _msgBack = request.downloadHandler.text;
                Debug.LogError(_msgBack);
            }

        }

        stopwatch.Stop();
        Debug.Log("����AI-��ʱ��" + stopwatch.Elapsed.TotalSeconds);
    }





    /// <summary>
    /// ����key
    /// </summary>
    private void SplitKey()
    {
        try {
            if (m_Key == "")
                return;

            string[] _split = m_Key.Split('.');
            m_ApiKey = _split[0];
            m_SecretKey = _split[1];
        } 
        catch { }


    }

    #region ����api��Ȩtoken

    /// <summary>
    /// ����api��Ȩ token
    /// </summary>
    /// <returns></returns>
    private string GetToken()
    {
        long expirationMilliseconds = DateTimeOffset.Now.AddHours(1).ToUnixTimeMilliseconds();
        long timestampMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        string jwtToken = GenerateJwtToken(m_ApiKey, expirationMilliseconds, timestampMilliseconds);
        return jwtToken;
    }
    //��ȡtoken
    private string GenerateJwtToken(string apiKeyId, long expirationMilliseconds, long timestampMilliseconds)
    {
        // ����Header
        string _headerJson = "{\"alg\":\"HS256\",\"sign_type\":\"SIGN\"}";

        string encodedHeader = Base64UrlEncode(_headerJson);

        // ����Payload
        string _playLoadJson = string.Format("{{\"api_key\":\"{0}\",\"exp\":{1}, \"timestamp\":{2}}}", apiKeyId, expirationMilliseconds, timestampMilliseconds);

        string encodedPayload = Base64UrlEncode(_playLoadJson);

        // ����ǩ��
        string signature = HMACsha256(m_SecretKey, $"{encodedHeader}.{encodedPayload}");
        // ���Header��Payload��Signature����JWT����
        string jwtToken = $"{encodedHeader}.{encodedPayload}.{signature}";

        return jwtToken;
    }
    // Base64 URL����
    private string Base64UrlEncode(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        string base64 = Convert.ToBase64String(inputBytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
    // ʹ��HMAC SHA256����ǩ��
    private string HMACsha256(string apiSecretIsKey, string buider)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(apiSecretIsKey);
        HMACSHA256 hMACSHA256 = new System.Security.Cryptography.HMACSHA256(bytes);
        byte[] date = Encoding.UTF8.GetBytes(buider);
        date = hMACSHA256.ComputeHash(date);
        hMACSHA256.Clear();

        return Convert.ToBase64String(date);

    }
    #endregion



    #region ���ݶ���
    [Serializable]
    public class PostData
    {
        public string model;
        public List<SendData> messages;
    }


    [Serializable]
    public class MessageBack
    {
        public string id;
        public string created;
        public string model;
        public List<MessageBody> choices;
    }
    [Serializable]
    public class MessageBody
    {
        public Message message;
        public string finish_reason;
        public string index;
    }
    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }


    #endregion

}
