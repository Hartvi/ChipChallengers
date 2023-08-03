using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayIntro : TopProp
{
    string[] keys = new string[] { "F1\n", "ESC" };
    string[] values = new string[] { "Show quick help", "Show menu" };
    BaseText[] textBases;
    BaseRawImage rawImageBase;
    void Start()
    {
        string[] z = new string[keys.Length + values.Length];
        keys.CopyTo(z, 0);
        values.CopyTo(z, keys.Length);
        textBases = GetComponentsInChildren<BaseText>();
        int i = 0;
        foreach(var textBase in textBases)
        {
            textBase.text.SetText(z[i]);
            i++;
        }
        rawImageBase = GetComponentInChildren<BaseRawImage>();

        Texture2D coverImg = Resources.Load("Images/chip_challengers") as Texture2D;
        //print("cover img: " + coverImg);
        //print("raw img base: " + rawImageBase);
        //print("raw image: " + rawImageBase.rawImage);
        rawImageBase.rawImage.texture = coverImg;
        float screenAvg = 0.7f * (Screen.height + Screen.width);
        rawImageBase.gameObject.RT().sizeDelta = new Vector2(screenAvg / 3f, screenAvg / 4f);
    }

}
