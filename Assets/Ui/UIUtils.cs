using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIUtils
{
    public static float SmallFontSize
    {
        get
        {
            return Screen.width * Screen.height / 1.5e5f;
        }
    }
    public static float MediumFontSize
    {
        get
        {
            return Screen.width * Screen.height / 1e5f;
        }
    }
    public static float LargeFontSize
    {
        get
        {
            return Screen.width * Screen.height / 7.5e4f;
        }
    }
    public static GameObject Panel, Button, Toggle, Image, Slider, Dropdown, Input, RawImage, ScrollView, Scrollbar, Text, FullscreenPanel;
    static UIUtils()
    {
        Panel = Resources.Load("UI/Panel") as GameObject;
        PRINT.print("INSTANTIATING UIUtils");
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
        FullscreenPanel = Resources.Load("UI/FullscreenPanel") as GameObject;
    }

}
