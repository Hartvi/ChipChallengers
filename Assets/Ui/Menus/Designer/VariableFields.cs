using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class VariableFields : BaseImage
{
    BaseText[] texts = new BaseText[5];
    BaseInput[] inputs = new BaseInput[5];
    BaseButton[] buttons = new BaseButton[2];

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Image, 1f, right,
            new VirtualProp(PropType.Panel, 0.5f, 
                new VirtualProp(PropType.Text, 1/6f),
                new VirtualProp(PropType.Text, 1/6f),
                new VirtualProp(PropType.Text, 1/6f),
                new VirtualProp(PropType.Text, 1/6f),
                new VirtualProp(PropType.Text, 1/6f),
                new VirtualProp(PropType.Button, -1f)
            ),
            new VirtualProp(PropType.Panel, 0.5f, 
                new VirtualProp(PropType.Input, 1/6f),
                new VirtualProp(PropType.Input, 1/6f),
                new VirtualProp(PropType.Input, 1/6f),
                new VirtualProp(PropType.Input, 1/6f),
                new VirtualProp(PropType.Input, 1/6f),
                new VirtualProp(PropType.Button, -1f)
            )
        );
    }

    void Start() {
        this.inputs = this.gameObject.GetComponentsInChildren<BaseInput>();
        this.texts = this.gameObject.GetComponentsInChildren<BaseText>();
        this.buttons = this.gameObject.GetComponentsInChildren<BaseButton>();

        for(int i = 0; i < this.texts.Length; ++i)
        {
            this.texts[i].text.SetText(UIStrings.VariableArray[i]);
        }

        for(int i = 0; i < this.buttons.Length; ++i)
        {
            this.buttons[i].text.SetText(UIStrings.AddDelete[i]);
        }

        this.SetTextSizesOf(this.inputs, UIUtils.SmallFontSize);
        this.SetTextSizesOf(this.texts, UIUtils.SmallFontSize);
        this.SetTextSizesOf(this.buttons, UIUtils.SmallFontSize);
    }

    public void PopulateFields(VirtualVariable virtualVariable)
    {
        string[] variableStrings = virtualVariable.ToStringArray();
        for (int i = 0; i < this.inputs.Length; ++i) {
            this.inputs[i].input.SetTextWithoutNotify(variableStrings[i]);
        }
    }

    void AddVariable()
    {
        string[] vals = this.inputs.Select(x => x.input.text).ToArray();
        VirtualVariable v = new VirtualVariable(vals);
        CommonChip.ClientCore.VirtualModel.AddVariable(v);
    }
}

