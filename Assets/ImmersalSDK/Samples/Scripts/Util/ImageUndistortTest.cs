using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Immersal;
using UnityEngine;
using UnityEngine.UI;

public class ImageUndistortTest : MonoBehaviour
{
    [SerializeField]
    private List<string> m_ImagePaths;
    
    [SerializeField]
    private Vector4 intrinsics = new Vector4(292.0501f, 292.1462f, 322.4464f, 237.4693f);
    
    [SerializeField]
    private Vector4 distCoeffs = new Vector4(0.1736287f, -0.2621811f, 0.2337622f, -0.06909606f);
    
    [SerializeField]
    private float alpha = 0.005648434f;
    
    [SerializeField]
    private RawImage m_InputImage = null;
    
    [SerializeField]
    private RawImage m_OutputImage = null;

    [SerializeField]
    private float m_UpdateTimeSpan = 2f;
    
    private float m_LastUpdateTime = 0f;
    private int m_CurrentIndex = 0;
    
    private Texture2D m_InputTex;
    private Texture2D m_OutputTex;

    private void Update()
    {
        float curTime = Time.unscaledTime;
        if (curTime > m_LastUpdateTime + m_UpdateTimeSpan)
        {
            m_LastUpdateTime = curTime;
            UndistortAtPath(m_ImagePaths[m_CurrentIndex++ % m_ImagePaths.Count]);
        }
    }

    public async void UndistortAtPath(string filename)
    {
        m_InputTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        string imagePath = Path.Combine(Application.dataPath, filename);
        byte[] bytes = File.ReadAllBytes(imagePath);
        m_InputTex.LoadImage(bytes);

        if (m_InputImage != null)
            m_InputImage.texture = m_InputTex;
        
        int width = m_InputTex.width;
        int height = m_InputTex.height;
        
        byte[] pixels = new byte[width * height];
        byte[] raw = m_InputTex.GetRawTextureData();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pixels[x + y * width] = (byte)(raw[(x + (height - 1 - y) * width) * 4 + 2]);
            }
        }

        int channels = 1;
        Vector4 intr = intrinsics;

        float startTime = Time.realtimeSinceStartup;

        Task<(byte[], int)> t = Task.Run(() =>
        {
            byte[] result = new byte[channels * width * height];
            int r = Immersal.Core.UndistortFishEyeImage(result, result.Length, pixels, width, height, channels, ref intr, ref distCoeffs, alpha);
            return (result, r);
        });

        await t;
        
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        
        Debug.Log($"Undistortion took {elapsedTime} with result {t.Result.Item2}");
        Debug.Log($"Undistorted intrinsics: {intr} ");

        byte[] output = ConvertToRGBAndFlipVertical(t.Result.Item1, width, height);
        
        m_OutputTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        m_OutputTex.LoadRawTextureData(output);
        m_OutputTex.Apply();

        if (m_OutputImage != null)
            m_OutputImage.texture = m_OutputTex;
    }
    
    private byte[] ConvertToRGBAndFlipVertical(byte[] originalData, int width, int height)
    {
        byte[] flippedRGBData = new byte[width * height * 3];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int originalIndex = (height - i - 1) * width + j;
                int newIndex = (i * width + j) * 3;
                flippedRGBData[newIndex] = originalData[originalIndex];
                flippedRGBData[newIndex + 1] = originalData[originalIndex];
                flippedRGBData[newIndex + 2] = originalData[originalIndex];
            }
        }
        return flippedRGBData;
    }
}
