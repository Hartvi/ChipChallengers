using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;

public static class UIUtils
{
    public static float SmallFontSize
    {
        get
        {
            return (Screen.width + Screen.height) / 1e2f;
        }
    }
    public static float MediumFontSize
    {
        get
        {
            return (2*Screen.width + Screen.height) / 7.5e1f;
        }
    }
    public static float LargeFontSize
    {
        get
        {
            return (2*Screen.width + Screen.height) / 5e1f;
        }
    }
    public static readonly GameObject Panel, Button, Toggle, Image, Slider, Dropdown, Input, RawImage, ScrollView, Scrollbar, Text;//, FullscreenPanel;
    public static readonly TMP_FontAsset DefaultFont;
    public static readonly Color DarkRed = 0.5f*Color.red + 0.5f*Color.black;
    public static readonly GameObject[] TextBearingUI;
    static UIUtils()
    {
        Panel = Resources.Load("UI/Panel") as GameObject;
        
        //PRINT.print("INSTANTIATING UIUtils");
        Button = Resources.Load("UI/Button") as GameObject;
        Toggle = Resources.Load("UI/Toggle") as GameObject;
        Image = Resources.Load("UI/Image") as GameObject;
        Slider = Resources.Load("UI/Slider") as GameObject;
        Dropdown = Resources.Load("UI/Dropdown") as GameObject;
        Input = Resources.Load("UI/InputField (TMP)") as GameObject;
        RawImage = Resources.Load("UI/RawImage") as GameObject;
        ScrollView = Resources.Load("UI/Scroll View") as GameObject;
        Scrollbar = Resources.Load("UI/Scrollbar") as GameObject;
        Text = Resources.Load("UI/Text (TMP)") as GameObject;

        TextBearingUI = new GameObject[]{ Button, Toggle, Dropdown, Input, Text };

        string mediumFontName = "DraftingMono-Medium SDF";
        DefaultFont = Resources.Load<TMP_FontAsset>($"Fonts/{mediumFontName}");
        foreach(var tbui in UIUtils.TextBearingUI)
        {
            //PRINT.print($"tbui: {tbui}");
            //UnityEngine.Debug.Log(tbui);
            //PRINT.print($"tbui txts:: {tbui.GetComponentsInChildren<TMP_Text>()}");
            foreach(var txt in tbui.GetComponentsInChildren<TMP_Text>())
            {
                UIUtils.SetFont(txt, mediumFontName);
                txt.color = UIUtils.DarkRed;
            }
        }

        //FullscreenPanel = Resources.Load("UI/FullscreenPanel") as GameObject;
    }
    public static void SetFont(TMP_Text textObject, string fontName)
    {
        TMP_FontAsset myFont = UIUtils.DefaultFont;

        textObject.font = myFont;

        textObject.ForceMeshUpdate();
    }

    public static string DisplayFloat(float f)
    {
        return (Mathf.Round(f * 100f) * 0.01f).ToString();
    }

}
