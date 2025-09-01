using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System;

public static class WavUtility
{
    /// <summary>
    /// 將 AudioClip 轉換為 WAV 格式的 byte 陣列
    /// </summary>
    public static byte[] FromAudioClip(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + clip.samples * 2);
        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((ushort)(clip.channels * 2));
        writer.Write((ushort)16);
        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
        writer.Write(clip.samples * 2);

        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        int intMax = 32767;

        for (int i = 0; i < clip.samples; i++)
        {
            writer.Write((short)(samples[i] * intMax));
        }

        writer.Close();
        byte[] wavBytes = stream.ToArray();
        stream.Close();
        return wavBytes;
    }

    /// <summary>
    /// 將 16-bit PCM byte[] 轉為 AudioClip
    /// </summary>
    public static AudioClip ConvertBytesToAudioClip(byte[] bytes, int sampleRate)
    {
        float[] floatArray = ConvertBytesToFloatArray(bytes);
        AudioClip audioClip = AudioClip.Create("GeneratedAudioClip", floatArray.Length, 1, sampleRate, false);
        audioClip.SetData(floatArray, 0);
        return audioClip;
    }

    /// <summary>
    /// 將 16-bit PCM byte[] 轉為 float[]（-1.0 ~ 1.0）
    /// </summary>
    public static float[] ConvertBytesToFloatArray(byte[] bytes)
    {
        float[] floatArray = new float[bytes.Length / 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            short value = BitConverter.ToInt16(bytes, i * 2);
            floatArray[i] = value / 32768.0f;
        }
        return floatArray;
    }

    /// <summary>
    /// 儲存 AudioClip 為 WAV 檔案
    /// </summary>
    public static void SaveAudioClip(AudioClip clip, string path, string name)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        byte[] byteArray = ConvertFloatArrayToByteArray(samples);
        string filePath = Path.Combine(path, name);

        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                WriteWavHeader(writer, clip);
                writer.Write(byteArray);
            }
        }

        Debug.Log("AudioClip saved at: " + filePath);
    }

    /// <summary>
    /// 將 float[]（-1.0 ~ 1.0）轉為 16-bit PCM byte[]
    /// </summary>
    public static byte[] ConvertFloatArrayToByteArray(float[] floatArray)
    {
        byte[] byteArray = new byte[floatArray.Length * 2];
        for (int i = 0; i < floatArray.Length; i++)
        {
            short value = (short)(floatArray[i] * 32767.0f);
            BitConverter.GetBytes(value).CopyTo(byteArray, i * 2);
        }
        return byteArray;
    }

    /// <summary>
    /// 寫入 WAV 檔案頭
    /// </summary>
    public static void WriteWavHeader(BinaryWriter writer, AudioClip clip)
    {
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + clip.samples * 2);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // Mono
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * 2); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample
        writer.Write("data".ToCharArray());
        writer.Write(clip.samples * 2);
    }
}

