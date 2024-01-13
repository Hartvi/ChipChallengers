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

        this.inputs[0].input.onEndEdit.RemoveAllListeners();
        this.inputs[0].input.onEndEdit.AddListener(
            x => {
                this.inputs[0].input.SetTextWithoutNotify(PermitOnlyVariables(x));
            }
        );
        for(int i = 1; i < this.inputs.Length; ++i)
        {
            int _i = i;
            this.inputs[i].input.onEndEdit.RemoveAllListeners();
            this.inputs[i].input.onEndEdit.AddListener(x => SanitizeInputField(this.inputs[_i].input, x));
        }

        for(int i = 0; i < this.texts.Length; ++i)
        {
            var t = this.texts[i];
            t.text.SetText(UIStrings.VariableArray[i]);
            t.text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            t.text.verticalAlignment = VerticalAlignmentOptions.Middle;

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

    public void PopulateFields(VVar virtualVariable)
    {
        string[] variableStrings = virtualVariable.ToStringArray();
        for (int i = 0; i < this.inputs.Length; ++i) {
            this.inputs[i].input.SetTextWithoutNotify(variableStrings[i]);
        }
    }

    public void AddVariable()
    {
        if (!this.inputs[0].input.text.IsVariableName())
        {
            DisplaySingleton.Instance.DisplayText(x =>
                {
                    DisplaySingleton.WarnMsgModification(x);
                    x.SetText($"Variable name {this.inputs[0].input.text} is invalid");
                }, 
                3f
            );
        }
        string[] vals = this.inputs.Select(x => x.input.text).ToArray();
        VVar v = new VVar(vals);
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
        string tmp = input;
        while (!IsFloat(tmp) && tmp.Length > 0)
        {
            tmp = tmp.Substring(0, Math.Max(0, tmp.Length - 1));
        }

        return tmp;
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
            UnityEngine.Debug.LogWarning($"Selecting the first input field is still not working for some reason after pressing enter.");
            this.inputs[0].input.Select();
        }
    }

    string PermitOnlyVariables(string input)
    {
        string tmp = input;
        while (!StringHelpers.IsVariableName(tmp) && tmp.Length > 0)
        {
            tmp = tmp.Substring(0, Math.Max(0, tmp.Length - 1));
        }

        return tmp;
    }
}

