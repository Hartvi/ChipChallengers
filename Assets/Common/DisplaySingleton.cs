using System;
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
            // Create a new GameObject with this component if none exists yet.
            if (instance == null)
            {
                var go = new GameObject("DisplaySingleton");
                instance = go.AddComponent<DisplaySingleton>();
            }
            return instance;
        }
    }

    [Serializable]
    private class DisplayedTextInfo
    {
        public TMP_Text TextComponent;   // the actual TMP_Text object
        public float Interval;          // how long it stays visible (0 or negative = forever)
        public float LastDisplayTime;   // the last time we called DisplayText on it
    }

    // Dictionary keyed by ID => text info
    private Dictionary<string, DisplayedTextInfo> displayedTexts = new Dictionary<string, DisplayedTextInfo>();

    // The Canvas we'll attach our new text objects to.
    private GameObject parentCanvas;

    private void Awake()
    {
        // Standard singleton pattern if you’re embedding in a scene.
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Find the first Canvas in the scene. Adjust if you want a more robust search.
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found in scene. DisplaySingleton requires a Canvas to place TMP_Text objects.");
        }
        else
        {
            parentCanvas = canvas.gameObject;
        }
    }

    private TMP_Text InitTxt()
    {
        // If you have a UIUtils.Text prefab or a static reference, adapt as needed:
        var txtObj = Instantiate(UIUtils.Text, parentCanvas.transform);
        var tmp = txtObj.GetComponent<TMP_Text>();
        return tmp;
    }

    /// <summary>
    /// Displays text on the screen. If you supply an ID, it reuses the same text object
    /// on subsequent calls with that same ID. If interval > 0, text will be hidden after that time.
    /// If interval <= 0, text stays forever (unless re-hidden manually).
    /// </summary>
    /// <param name="modification">Callback to modify the TMP_Text (font size, color, etc.)</param>
    /// <param name="interval">Time in seconds to remain visible (> 0 means auto-hide, <= 0 means stay)</param>
    /// <param name="id">Optional unique identifier. If omitted or empty, a new ID is generated each time.</param>
    public void DisplayText(Action<TMP_Text> modification, float interval, string id = "")
    {
        if (parentCanvas == null)
        {
            Debug.LogWarning("No parent canvas found; cannot display text.");
            return;
        }

        DisplayedTextInfo info;
        if (!displayedTexts.TryGetValue(id, out info))
        {
            // Create a new text object
            TMP_Text txt = InitTxt();

            info = new DisplayedTextInfo
            {
                TextComponent = txt,
                Interval = interval,
                LastDisplayTime = Time.time
            };

            displayedTexts[id] = info;
        }
        else
        {
            // Reuse the existing text object
            info.Interval = interval;
            info.LastDisplayTime = Time.time;
        }

        // Apply user-defined modifications (color, font size, alignment, etc.)
        modification(info.TextComponent);

        // Make sure it’s active and visible
        info.TextComponent.gameObject.SetActive(true);
    }

    private void Update()
    {
        float currentTime = Time.time;

        // Iterate over all displayed texts
        foreach (var kvp in displayedTexts)
        {
            DisplayedTextInfo info = kvp.Value;
            // If interval > 0, we auto-hide after 'interval' seconds
            if (info.Interval > 0f)
            {
                if (currentTime - info.LastDisplayTime >= info.Interval)
                {
                    // Hide it
                    if (info.TextComponent != null)
                    {
                        info.TextComponent.gameObject.SetActive(false);
                    }
                }
            }
            // If interval <= 0, it stays forever (do nothing here).
        }
    }

    #region Example Modifications

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
        // Position the text around bottom-center
        RectTransform rt = txt.gameObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(Screen.width / 2f, Screen.height / 3f);
        }
    }

    public static void NoOverflowEtc(TMP_Text txt)
    {
        txt.enableWordWrapping = false;
        txt.alignment = TextAlignmentOptions.Center;
    }

    public static void ErrorMsgModification(TMP_Text txt)
    {
        BasicRedModification(txt);
        BasicSmallModification(txt);
        BasicBottomModification(txt);
        NoOverflowEtc(txt);
    }

    public static void WarnMsgModification(TMP_Text txt)
    {
        BasicOrangeModification(txt);
        BasicSmallModification(txt);
        BasicBottomModification(txt);
        NoOverflowEtc(txt);
    }

    #endregion
}
