using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Sirenix.OdinInspector;

public class AudioVisuals : MonoBehaviour
{

    [SerializeField]
    AudioReader audioReader;

    [SerializeField]
    Transform[] Bars;

    [SerializeField]
    bool AutoBuild = false;

    [SerializeField]
    bool Circle = false;

    [SerializeField]
    float multiplier = 1;

    [SerializeField]
    float BarMod = 1;

    [SerializeField]
    Transform BarParent;

    [SerializeField, FoldoutGroup("Stats"), Range(0.01f, 1f)]
    float Delay = 1;

    [SerializeField, FoldoutGroup("Stats"), Range(0.01f, 1f)]
    float Decay = 1;

    [SerializeField, FoldoutGroup("Stats")]
    float MaxHeight = 1;

    [SerializeField, FoldoutGroup("Stats")]
    bool Position;

    [SerializeField, FoldoutGroup("Stats")]
    bool Scale;


    [SerializeField, FoldoutGroup("BarBuilder")]
    int NumOfBars = 100;

    [SerializeField, FoldoutGroup("BarBuilder")]
    float BarSize = 1;

    [SerializeField, FoldoutGroup("BarBuilder")]
    float BarSpace = 1;

    [SerializeField, FoldoutGroup("BarBuilder")]
    GameObject Prefab;

    [SerializeField, ReadOnly, FoldoutGroup("RO")]
    float[] VisHeight;
    [SerializeField, ReadOnly, FoldoutGroup("RO")]
    float[] BarHeight;
    [SerializeField, ReadOnly, FoldoutGroup("RO")]
    Vector3[] BarInitPos;
    [SerializeField, ReadOnly, FoldoutGroup("RO")]
    int SpecCount;
    [SerializeField, ReadOnly, FoldoutGroup("RO")]
    int Barcount;
    // Start is called before the first frame update
    void Start()
    {
        if (!AutoBuild)
        {
            if (BarParent != null)
            {
                List<Transform> ChildTransforms = new List<Transform>();

                for (int i = BarParent.childCount - 1; i >= 0; i--)
                {
                    ChildTransforms.Add(BarParent.GetChild(i));
                }

                Bars = ChildTransforms.ToArray();
            }
        }
        else
        {
            Bars = new Transform[0];
            rebuild();
        }

        SpecCount = audioReader.SpecData.Length;
        Barcount = Bars.Length;
        VisHeight = new float[Bars.Length];
        BarHeight = new float[Bars.Length];
    }

    // Update is called once per frame
    void Update()
    {
        int SampleSize = Mathf.FloorToInt(SpecCount / Barcount);

        for (int i = 0; i < Barcount; i++)
        {
            float avg = 0;

            for (int j = 0; j < SampleSize * 2; j++)
            {
                avg += Mathf.Log(i * BarMod) * audioReader.SpecData[(int)((i * 0.5f) * SampleSize) + j];
            }

            VisHeight[i] = Mathf.Clamp((avg / SampleSize) * multiplier, 0, MaxHeight);
        }

        float[] TempVals = new float[Barcount];

        for (int i = 3; i < Barcount - 3; i++)
        {
            TempVals[i] = (VisHeight[i - 3] + VisHeight[i - 2] + VisHeight[i - 1] + VisHeight[i] + VisHeight[i + 1] + VisHeight[i + 2] + VisHeight[i + 3]) / 7f;

            TempVals[i] = VisHeight[i];
        }

        VisHeight = TempVals;

        for (int i = 0; i < Barcount; i++)
        {
            if (VisHeight[i] > BarHeight[i])
                BarHeight[i] = Mathf.Lerp(BarHeight[i], VisHeight[i], Delay);
            else
                BarHeight[i] = Mathf.Lerp(BarHeight[i], VisHeight[i], Decay);

            if (Position)
                Bars[i].position = BarInitPos[i] + (Bars[i].up * BarHeight[i] * 0.5f);
            if (Scale)
                Bars[i].localScale = new Vector3(Bars[i].localScale.x, BarHeight[i], Bars[i].localScale.z);


            if (i != 0)
            {
                Debug.DrawLine(Bars[i].position, Bars[i - 1].position);
            }
        }
    }

    [Button]
    void rebuild()
    {


        if (Bars != null)
            foreach (var bar in Bars)
            {
                DestroyImmediate(bar.gameObject);
            }

        List<Transform> ChildTransforms = new List<Transform>();
        for (int i = 0; i < NumOfBars; i++)
        {
            GameObject obj = GameObject.Instantiate(Prefab, this.transform);

            ChildTransforms.Add(obj.transform);

            obj.transform.position = new Vector3(this.transform.position.x + (i * (BarSize + BarSpace)), obj.transform.position.y, obj.transform.position.z);
            obj.transform.localScale *= BarSize;
            if (Circle)
                obj.transform.eulerAngles = new Vector3(0, 0, i * (360 / NumOfBars));
        }

        Bars = ChildTransforms.ToArray();
     
        //SpecCount = audioReader.SpecData.Length;
        Barcount = Bars.Length;
        VisHeight = new float[Bars.Length];
        BarHeight = new float[Bars.Length];
        BarInitPos = ChildTransforms.Select(t => t.position).ToArray();
    }
}
