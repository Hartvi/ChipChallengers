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

        for(int i = 1; i < this.inputs.Length; ++i)
        {
            int _i = i;
            this.inputs[i].input.onValueChanged.AddListener(x => SanitizeInputField(this.inputs[_i].input, x));
        }

        for(int i = 0; i < this.texts.Length; ++i)
        {
            this.texts[i].text.SetText(UIStrings.VariableArray[i]);
        }

        for(int i = 0; i < this.buttons.Length; ++i)
        {
            this.buttons[i].text.SetText(UIStrings.AddDelete[i]);
        }

        this.buttons[0].btn.onClick.AddListener(AddVariable);
        this.buttons[1].btn.onClick.AddListener(DeleteSelectedVariable);

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

    public void AddVariable()
    {
        string[] vals = this.inputs.Select(x => x.input.text).ToArray();
        VirtualVariable v = new VirtualVariable(vals);
        CommonChip.ClientCore.VirtualModel.AddAndSelectVariable(v);
        // add variable, select it
        // update the display panel for variables
    }

    public void DeleteSelectedVariable()
    {
        CommonChip.ClientCore.VirtualModel.DeleteSelectedVariable();
    }

    private void SanitizeInputField(TMP_InputField inputField, string input)
    {
        inputField.SetTextWithoutNotify(ValidateFloatInput(input));
    }

    private string ValidateFloatInput(string input)
    {
        if (!IsFloat(input))
        {
            return input.Substring(0, Math.Max(0, input.Length - 1));
        }

        return input;
    }

    private bool IsFloat(string value)
    {
        return float.TryParse(value, out _);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift))
        {
            bool wasFocused = false;
            for (int i = this.inputs.Length - 1; i > -1; --i)
            {
                if (this.inputs[i].input.isFocused)
                {
                    this.inputs[(i - 1 + this.inputs.Length) % this.inputs.Length].input.Select();
                    wasFocused = true;
                    break;
                }
            }
            if (!wasFocused)
            {
                this.inputs[0].input.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool wasFocused = false;
            for (int i = 0; i < this.inputs.Length; ++i)
            {
                if (this.inputs[i].input.isFocused)
                {
                    this.inputs[(i + 1) % this.inputs.Length].input.Select();
                    wasFocused = true;
                    break;
                }
            }
            if (!wasFocused)
            {
                this.inputs[0].input.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            for(int i = 0; i < this.inputs.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(this.inputs[i].input.text))
                {
                    this.inputs[i].input.SetTextWithoutNotify(UIStrings.DefaultVariableValues[i]);
                }
            } 
            AddVariable();
            // TODO: doesn't work ofc, somehow it selects the second guy instead
            this.inputs[0].input.Select();
        }
    }

}

