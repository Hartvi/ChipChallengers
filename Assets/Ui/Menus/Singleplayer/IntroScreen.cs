using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class IntroScreen : BasePanel
{
    TMP_Text[] texts;

    void Start()
    {
        texts = GetComponentsInChildren<TMP_Text>();
        texts[1].text = UIStrings.IntroKeys;
        texts[1].fontSize = UIUtils.MediumFontSize;
        //texts[0].horizontalAlignment = HorizontalAlignmentOptions.Center;
        texts[2].text = UIStrings.IntroValues;
        texts[2].fontSize = UIUtils.MediumFontSize;
        //texts[1].horizontalAlignment = HorizontalAlignmentOptions.Center;
    }
    void Update()
    {
        if (Input.anyKeyDown)
        {
            this.gameObject.SetActive(false);
        }
    }
}
