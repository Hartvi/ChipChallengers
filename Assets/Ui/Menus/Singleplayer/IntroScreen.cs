using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class IntroScreen : BasePanel
{
    float startTime;
    const float IntroTime = 3000f;

    TMP_Text[] texts;
    void Start()
    {
        startTime = Time.time;
        texts = GetComponentsInChildren<TMP_Text>();
        texts[0].text = UIStrings.IntroKeys;
        texts[0].fontSize = UIUtils.SmallFontSize;
        texts[0].horizontalAlignment = HorizontalAlignmentOptions.Center;
        texts[1].text = UIStrings.IntroValues;
        texts[1].fontSize = UIUtils.SmallFontSize;
        texts[1].horizontalAlignment = HorizontalAlignmentOptions.Center;
    }
    void Update()
    {
        if(Time.time - startTime > IntroTime)
        {
            this.gameObject.SetActive(false);
        }
    }
}
