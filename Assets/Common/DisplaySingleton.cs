using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DisplaySingleton : MonoBehaviour
{
    private static DisplaySingleton instance;

    public static DisplaySingleton Instance
    {
        get
        {
            return DisplaySingleton.instance ?? (DisplaySingleton.instance = new GameObject().AddComponent<DisplaySingleton>());
        }
    }

    ObjectPool<TMP_Text> textPool;

    GameObject parent;
    //TMP_Text text;
    float[] lastTime, interval;
    private int numberOfTexts = 10;

    void Awake()
    {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        this.parent = canvas.gameObject;
        //this.text = Instantiate(UIUtils.Text).GetComponent<TMP_Text>();
        //this.text.transform.SetParent(this.parent.transform);
        this.textPool = new ObjectPool<TMP_Text>(this.numberOfTexts, () =>
            {
                var txt = Instantiate(UIUtils.Text).GetComponent<TMP_Text>();
                txt.transform.SetParent(this.parent.transform);
                return txt;
            },
            (x) => { GameObject.Destroy(x.gameObject); }
        );
        this.lastTime = new float[this.numberOfTexts];
        this.interval = new float[this.numberOfTexts];
    }

    public void DisplayText(Action<TMP_Text> modification, float interval)
    {
        TMP_Text txt = this.textPool.Next();
        // do not overwrite existing text
        if (txt.gameObject.activeSelf) { return; }
        modification(txt);
        txt.gameObject.SetActive(true);
        this.lastTime[this.textPool.currentIndex] = Time.time;
        this.interval[this.textPool.currentIndex] = interval;
    }

    public static void BasicLargeModification(TMP_Text txt)
    {
        txt.fontSize = UIUtils.LargeFontSize;
    }

    public static void BasicMediumModification(TMP_Text txt)
    {
        txt.fontSize = UIUtils.MediumFontSize;
    }

    public static void BasicSmallModification(TMP_Text txt)
    {
        txt.fontSize = UIUtils.SmallFontSize;
    }

    public static void BasicOrangeModification(TMP_Text txt)
    {
        txt.color = Color.red + 0.5f * Color.green;
    }

    public static void BasicRedModification(TMP_Text txt)
    {
        txt.color = Color.red;
    }

    public static void BasicBottomModification(TMP_Text txt)
    {
        txt.gameObject.RT().position = new Vector3(Screen.width / 2f, Screen.height / 3f, 0f);
    }

    public static void NoOverflowEtc(TMP_Text txt)
    {
        txt.enableWordWrapping = false;
        txt.alignment = TextAlignmentOptions.Center;
    }

    public static void ErrorMsgModification(TMP_Text txt)
    {
        DisplaySingleton.BasicRedModification(txt);
        DisplaySingleton.BasicSmallModification(txt);
        DisplaySingleton.BasicBottomModification(txt);
        DisplaySingleton.NoOverflowEtc(txt);
    }
    public static void WarnMsgModification(TMP_Text txt)
    {
        DisplaySingleton.BasicOrangeModification(txt);
        DisplaySingleton.BasicSmallModification(txt);
        DisplaySingleton.BasicBottomModification(txt);
        DisplaySingleton.NoOverflowEtc(txt);
    }

    void Update()
    {
        for (int i = 0; i < this.textPool.objects.Length; ++i)
        {
            if ((Time.time - this.lastTime[i]) > this.interval[i])
            {
                this.textPool.objects[i].gameObject.SetActive(false);
            }
        }
    }

}
