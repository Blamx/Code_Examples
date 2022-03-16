using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Sirenix.OdinInspector;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Asyncoroutine;


public class AverageColor : Singleton<AverageColor>
{
    [SerializeField]
    RenderTexture capturedFrame;

    Camera camera;

    [SerializeField]
    RawImage Preview;

    Texture2D m_LastCameraTexture;

    Texture2D colorPreview;

    Texture2D colorPreview2;

    [SerializeField]
    RawImage colorPreviewImage;

    [SerializeField]
    RawImage colorPreviewImageMid;

    [SerializeField]
    Light spotLight;

    Color skyLight;

    float intensity = 1;

    public Color top { get; private set; } = new Color();
    public Color mid { get; private set; } = new Color();
    public Color low { get; private set; } = new Color();

    public Color oldTop = new Color();
    public Color oldMid = new Color();
    public Color oldLow = new Color();

    float timer;

    bool running = true;

    // Start is called before the first frame update
    void Start()
    {
        skyLight = spotLight.color;
        camera = Camera.main;

        if (m_LastCameraTexture == null)
            m_LastCameraTexture = new Texture2D(capturedFrame.width, capturedFrame.height, TextureFormat.RGB24, true);

        calculateLightingAsync();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer < 1.1f)
        {
            RenderSettings.ambientSkyColor = Color.Lerp(oldTop, top, timer);
            RenderSettings.ambientEquatorColor = Color.Lerp(oldMid, mid, timer);
            RenderSettings.ambientGroundColor = Color.Lerp(oldLow, low, timer);
        }
    }

    async Task getScreenValuesAsync(Texture2D tex)
    {
        //declaration
        int Xsize = 6, Ysize = 6;
      
        Color[] pixelGroup;
        Color[,] blocks = new Color[Xsize, Ysize];
        Texture2D colTex = new Texture2D(Xsize, Ysize);

        Dictionary<Vector2, Task<Color>> BlockTasks = new Dictionary<Vector2, Task<Color>>();

        Task[] t = new Task[10];

        for (int i = 0; i < Xsize; i++)
        {
            for (int j = 0; j < Ysize; j++)
            {


                pixelGroup = tex.GetPixels((tex.width / Xsize) * i, (tex.height / Ysize) * j, tex.width / Xsize, tex.height / Ysize);

                BlockTasks.Add(new Vector2(i, j), Task<Color>.Run(() => getAverageColor(pixelGroup)));

                await new WaitForNextFrame();
                //blocks[i, j] = getAverageColor(pixelGroup);
            }
        }

        await Task.WhenAll(BlockTasks.Values);

        foreach (var pair in BlockTasks)
        {
            //var value = await pair.Value;
            blocks[(int)pair.Key.x, (int)pair.Key.y] = await pair.Value;
        }

        oldTop = top;
        oldMid = mid;
        oldLow = low;


        //second less big math
        low = calculateGround(blocks);

        top = calculateSky(blocks);

        mid = calculateMid(top, low, 0.72f);

        low *= 0.5f;

        top *= 1.5f;

        timer = 0;

        //setting lighting
        //RenderSettings.ambientSkyColor = top;
        //RenderSettings.ambientEquatorColor = mid;
        //RenderSettings.ambientGroundColor = low;

    }

    Color getAverageColor(Color[] colors)
    {
        float r = 0, g = 0, b = 0;

        for (int i = 0; i < colors.Length; i++)
        {
            r += colors[i].r;

            g += colors[i].g;

            b += colors[i].b;

        }

        //Log.addToLog(r + "," + g + "," + b);

        return new Color(r / colors.Length, g / colors.Length, b / colors.Length);
    }

    Color getAverageColor(Color[,] colors)
    {
        float r = 0, g = 0, b = 0;
        for (int i = 0; i < colors.GetLength(0); i++)
        {
            for (int j = 0; j < colors.GetLength(1); j++)
            {
                r += colors[i, j].r;

                g += colors[i, j].g;

                b += colors[i, j].b;

            }
        }

        return new Color(r / colors.Length, g / colors.Length, b / colors.Length);
    }

    Color calculateSky(Color[,] Blocks)
    {

        Color t = getAverageColor(Blocks) * (1f + getBrightest(Blocks));

        t.a = 1;

        skyLight = t;

        return t;
    }

    Color calculateMid(Color top, Color low, float lerpVal)
    {
        return Color.Lerp(top, low, lerpVal);
    }

    Color calculateGround(Color[,] blocks)
    {
        return getAverageColor(blocks);
    }

    public void captureFrame()
    {
        // Copy the camera background to a RenderTexture
        //calculateLightingAsync();
    }

    float getBrightest(Color[,] Blocks)
    {

        float HighestV = 0;
        Color HighestI = new Color();
        float h, s, v;
        foreach (Color item in Blocks)
        {
            Color.RGBToHSV(item, out h, out s, out v);
            if (v > HighestV)
            {
                HighestV = v;
                HighestI = item;
            }
        }

        intensity = HighestV;

        return HighestV;
    }

    async void calculateLightingAsync()
    {
        while (running)
        {
            await new WaitForSecondsRealtime(1f);

            Graphics.Blit(null, capturedFrame, camera.GetComponent<ARCameraBackground>().material);

            await GetFrame();

            await getScreenValuesAsync(m_LastCameraTexture);

            spotLight.color = skyLight;
            spotLight.intensity = 1 + (intensity * 15);
        }
    }

    public Texture2D ToTexture2D(Texture texture)
    {
        Texture2D t = new Texture2D(texture.width, texture.height);

        t.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        t.Apply();


        return t;
    }

    async Task GetFrame()
    {
        await new WaitForEndOfFrame();

        ////// Copy the RenderTexture from GPU to CPU
        var activeRenderTexture = RenderTexture.active;
        RenderTexture.active = capturedFrame;
        m_LastCameraTexture.ReadPixels(new Rect(0, 0, capturedFrame.width, capturedFrame.height), 0, 0);
        m_LastCameraTexture.Apply();
        RenderTexture.active = activeRenderTexture;

        //Preview.texture = m_LastCameraTexture;
    }
}
