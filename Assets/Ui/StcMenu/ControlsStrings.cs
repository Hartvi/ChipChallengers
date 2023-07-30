using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControlsStrings : TopProp
{
    BaseText[] texts;
    protected void Start()
    {
        //texts = GetChildren<BaseText>();
        texts = GetComponentsInChildren<BaseText>();
        texts[0].text.SetText("General Controls");
        texts[0].text.alignment = TextAlignmentOptions.Top;
        texts[1].text.fontSize = UIUtils.MediumFontSize;
        texts[2].text.fontSize = UIUtils.MediumFontSize;
        texts[1].text.SetText(
                "Ctrl+R: reset model\n" +
                "Ctrl+U: reset model and go to default position\n" +
                "F1: show model controls\n" +
                "Ctrl+O: open model\n" +
                "Ctrl+M: load map\n" +
                "Ctrl+E: go to model editor\n" +
                "Ctrl+L: load time trial\n" +
                "Ctrl+T: time trial creator mode\n" +
                "Ctrl+S (in time trial creator mode): save the time trial\n" +
                "Ctrl+arrows: Adjust camera position\n" +
                "Ctrl+'+'/'-': Adjust camera distance\n" +
                "Ctrl+0: Reset camera\n"
                        );
        texts[2].text.SetText(
                "Ctrl+P: go to play mode\n"+
                "Ctrl+Shift+P: go to playmode without saving model\n"+
                "Ctrl+S: save model\n"+
                "Ctrl+Shift+S: save model as\n"+
                "F1: Orbital camera\n"+
                "F2: Free camera\n"
            );
        //print(texts[2].text + ": " + texts[2].gameObject.name);
    }

}
