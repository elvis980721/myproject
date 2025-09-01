using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_WEBGL
public class Audio2LipScript : MonoBehaviour
{
    [Tooltip("選擇用於計算 Viseme 的唇型同步處理方式")]
    public OVRLipSync.ContextProviders provider = OVRLipSync.ContextProviders.Enhanced;

    [Tooltip("是否啟用在支援的 Android 裝置上的 DSP 加速")]
    public bool enableAcceleration = true;

    [SerializeField] private uint Context = 0;
    [SerializeField] public float gain = 1.0f;

    /// <summary>
    /// 音訊來源
    /// </summary>
    [SerializeField] private AudioSource m_AudioSource;

    /// <summary>
    /// 擁有 BlendShape 的 SkinnedMeshRenderer
    /// </summary>
    [Header("設定含有 BlendShape 的 SkinnedMeshRenderer")]
    public SkinnedMeshRenderer meshRenderer;

    /// <summary>
    /// BlendShape 權重倍率
    /// </summary>
    public float blendWeightMultiplier = 100f;

    /// <summary>
    /// 設定每個 Viseme 對應的 BlendShape 索引
    /// </summary>
    [Header("設定各個母音對應的 BlendShape 索引")]
    public VisemeBlenderShapeIndexMap m_VisemeIndex;

    /// <summary>
    /// 音素分析結果
    /// </summary>
    private OVRLipSync.Frame frame = new OVRLipSync.Frame();
    protected OVRLipSync.Frame Frame => frame;

    private void Awake()
    {
        m_AudioSource = this.GetComponent<AudioSource>();
        if (Context == 0)
        {
            if (OVRLipSync.CreateContext(ref Context, provider, 0, enableAcceleration) != OVRLipSync.Result.Success)
            {
                Debug.LogError("OVRLipSyncContextBase.Start 錯誤：無法建立音素分析 context。");
                return;
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        ProcessAudioSamplesRaw(data, channels);
    }

    /// <summary>
    /// 將 F32 PCM 音訊資料傳入唇型同步模組
    /// </summary>
    public void ProcessAudioSamplesRaw(float[] data, int channels)
    {
        lock (this)
        {
            if (OVRLipSync.IsInitialized() != OVRLipSync.Result.Success)
                return;

            OVRLipSync.ProcessFrame(Context, data, Frame, channels == 2);
        }
    }

    private void Update()
    {
        if (Frame != null)
        {
            SetBlenderShapes();
        }
    }

    private void SetBlenderShapes()
    {
        for (int i = 0; i < Frame.Visemes.Length; i++)
        {
            string name = ((OVRLipSync.Viseme)i).ToString();
            int blendShapeIndex = GetBlenderShapeIndexByName(name);
            int blendWeight = (int)(blendWeightMultiplier * Frame.Visemes[i]);

            if (blendShapeIndex == 999)
                continue;

            meshRenderer.SetBlendShapeWeight(blendShapeIndex, blendWeight);
        }
    }

    /// <summary>
    /// 依照音素名稱回傳對應的 BlendShape 索引
    /// </summary>
    private int GetBlenderShapeIndexByName(string name)
    {
        return name switch
        {
            "sil" => 999,
            "aa" => m_VisemeIndex.A,
            "ih" => m_VisemeIndex.I,
            "E" => m_VisemeIndex.E,
            "oh" => m_VisemeIndex.O,
            _ => m_VisemeIndex.U,
        };
    }

    [System.Serializable]
    public class VisemeBlenderShapeIndexMap
    {
        public int A;
        public int I;
        public int U;
        public int E;
        public int O;
    }
}
#endif
