using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class VariablePanel : BasePanel
{
    public const int NUMDISPLAYITEMS = 7;
    BaseItemScroll itemScroll;
    VariableFields variableFields;
    //TMP_Text[] texts = new TMP_Text[5];
    //TMP_InputField[] inputs = new TMP_InputField[5];

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Panel, 0.5f, right,
                new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                    new VirtualProp(PropType.Button, 1f)
                ),
                new VirtualProp(PropType.Scrollbar, -1f, right, right)
            ),
            new VirtualProp(PropType.Image, -1f, typeof(VariableFields))
        );
    }
    void Start()
    {
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>();
        Action<string> action = this.SelectVariable;
        var baseScroll = this.GetComponentInChildren<BaseItemScroll>();
        print($"basescroll: {baseScroll}");

        baseScroll.SetupItemList(action, VariablePanel.NUMDISPLAYITEMS, new string[] { });

        var scrollbar = baseScroll.Siblings<BaseScrollbar>(false)[0];

        scrollbar.scrollbar.onValueChanged.AddListener(baseScroll.Scroll);
        baseScroll.Scroll(0f);
        scrollbar.scrollbar.value = 0f;

        this.variableFields = this.GetComponentInChildren<VariableFields>();

        //Action<string> action = x => UnityEngine.Debug.Log(x);
        //int numItems = 7;
        //string[] items = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        //var baseScroll = this.GetComponentInChildren<BaseItemScroll>();
        //print($"basescroll: {baseScroll}");
        //baseScroll.SetupItemList(action, numItems, items);
        //var scrollbar = baseScroll.Siblings<BaseScrollbar>(false)[0];
        //scrollbar.scrollbar.onValueChanged.AddListener(baseScroll.Scroll);
        //baseScroll.Scroll(0f);
        //scrollbar.scrollbar.value = 0f;
    }

    public void SelectVariable(string variableName)
    {
        CommonChip clientCore = CommonChip.ClientCore;
        clientCore.VirtualModel.SetSelectedVariable(variableName);
        this.variableFields.PopulateFields(clientCore.VirtualModel.GetSelectedVariable());
    }


}
