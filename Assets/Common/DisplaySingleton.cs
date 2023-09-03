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

    GameObject parent;
    TMP_Text text;
    float lastTime, interval;

    void Awake()
    {
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        this.parent = canvas.gameObject;
        this.text = Instantiate(UIUtils.Text).GetComponent<TMP_Text>();
        this.text.transform.SetParent(this.parent.transform);
    }

    public void DisplayText(Action<TMP_Text> modification, float interval)
    {
        this.lastTime = Time.time;
        this.interval = interval;
        modification(this.text);
        this.gameObject.SetActive(true);
        this.text.gameObject.SetActive(true);
    }

    public static void BasicLargeModification(TMP_Text txt)
    {
        txt.fontSize = UIUtils.LargeFontSize;
    }

    public static void BasicMediumModification(TMP_Text txt)
    {
        txt.fontSize = UIUtils.MediumFontSize;
    }

    public static void BasicRedModification(TMP_Text txt)
    {
        txt.color = Color.red;
    }

    public static void BasicBottomModification(TMP_Text txt)
    {
        txt.gameObject.RT().position = new Vector3(Screen.width / 2f, Screen.height / 10f, 0f);
    }

    public static void NoOverflowEtc(TMP_Text txt)
    {
        txt.enableWordWrapping = false;
        txt.alignment = TextAlignmentOptions.Center;
    }

    public static void ErrorMsgModification(TMP_Text txt)
    {
        DisplaySingleton.BasicRedModification(txt);
        DisplaySingleton.BasicMediumModification(txt);
        DisplaySingleton.BasicBottomModification(txt);
        DisplaySingleton.NoOverflowEtc(txt);
    }

    void Update()
    {
        if(Time.time - this.lastTime > this.interval)
        {
            this.gameObject.SetActive(false);
            this.text.gameObject.SetActive(false);
        }
    }

}
