using System.Collections;
using System.Collections.Generic;
using System.Linq;


using UnityEngine;

using Sirenix.OdinInspector;

public class AudioReader : MonoBehaviour
{
    [SerializeField]
    bool ShowDebug;

    [SerializeField, ShowIf("ShowDebug")]
    float DebugMag = 1;

    [SerializeField]
    bool RawData;


    [SerializeField]
    AudioSource Audio;
    [SerializeField]
    public float[] SpecData = new float[512];

    [SerializeField]
    int SpecSize = 512;

    [SerializeField]
    FFTWindow type;

    [SerializeField]
    int channel;

    [SerializeField]
    float Cutoff = 1;

    private void Awake()
    {
        SpecData = new float[SpecSize];
    }

    // Update is called once per frame
    void Update()
    {

        float[] SpecDataOut = new float[SpecSize];

        if (!RawData)
            Audio.GetSpectrumData(SpecDataOut, channel, type);
        else
            Audio.GetOutputData(SpecData, 0);

        int s = (int)(SpecDataOut.Length * Cutoff);

        SpecData = SpecDataOut.Take(s).ToArray();
        

        if (ShowDebug)
            DrawDebug();

    }

    void DrawDebug()
    {
        for (int i = 1; i < SpecData.Length; i++)
        {
            Debug.DrawLine(new Vector3((i - 1) * 0.1f, Mathf.Log10(i - 1) * SpecData[i - 1] * DebugMag, 0), new Vector3(i * 0.1f, Mathf.Log10(i) * SpecData[i] * DebugMag, 0));
        }
    }
}
