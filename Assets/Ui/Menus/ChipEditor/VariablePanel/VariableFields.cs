using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Mirror;

public class VariableFields : BaseImage
{
    BaseText[] texts = new BaseText[5];
    BaseInput[] inputs = new BaseInput[5];
    BaseButton[] buttons = new BaseButton[2];
    bool initialized;
    bool focusNameAfterSubmit;

    public override void Setup()
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

    void Start() => this.InitializeControls();

    // InitializeControls configures variable fields, labels and button listeners once.
    void InitializeControls()
    {
        if (this.initialized) { return; }
        this.inputs = this.gameObject.GetComponentsInChildren<BaseInput>();
        this.texts = this.gameObject.GetComponentsInChildren<BaseText>();
        this.buttons = this.gameObject.GetComponentsInChildren<BaseButton>();

        for(int i = 0; i < this.inputs.Length; ++i)
        {
            int _i = i;
            this.inputs[i].input.onEndEdit.RemoveAllListeners();
            this.inputs[i].input.onEndEdit.AddListener(x => this.EndEditField(this.inputs[_i].input, x));
        }
        foreach (var field in this.inputs)
            field.input.onSubmit.AddListener(_ => this.SubmitVariable());

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
        this.initialized = true;
    }

    public void PopulateFields(VVar virtualVariable)
    {
        this.InitializeControls();
        string[] variableStrings = virtualVariable.ToStringArray();
        for (int i = 0; i < this.inputs.Length; ++i) {
            this.inputs[i].input.SetTextWithoutNotify(variableStrings[i]);
        }
    }

    public void AddVariable()
    {
        if (!ModelValidation.IsVariableName(this.inputs[0].input.text))
        {
            this.ShowError("Choose a valid Lua variable name.");
            return;
        }
        for (int i = 1; i < this.inputs.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(this.inputs[i].input.text))
                this.inputs[i].input.SetTextWithoutNotify(UIStrings.DefaultVariableValues[i]);
        }
        string[] vals = this.inputs.Select(x => x.input.text).ToArray();
        try
        {
            VVar v = new VVar(vals);
            var model = CoreChip.ClientCoreChip.VirtualModel;
            var existing = model.variables.FirstOrDefault(variable => variable.name == v.name);
            if (existing != null && existing.ToStringArray().SequenceEqual(v.ToStringArray()))
            {
                if (!ReferenceEquals(model.GetSelectedVariable(), existing)) { model.SetSelectedVariable(v.name); }
                return;
            }
            model.AddAndSelectVariable(v);
        }
        catch (Exception error) when (error is ArgumentException || error is FormatException)
        {
            this.ShowError(error.Message);
        }
        // add variable, select it
        // update the display panel for variables
    }

    public void DeleteSelectedVariable()
    {
        CoreChip.ClientCoreChip.VirtualModel.DeleteSelectedVariable();
    }

    void EndEditField(TMP_InputField inputField, string value)
    {
        if (!this.initialized || inputField.wasCanceled ||
            NetworkClient.localPlayer == null) { return; }
        if (inputField != this.inputs[0].input && !this.SanitizeInputField(inputField, value)) { return; }
        if (string.IsNullOrWhiteSpace(this.inputs[0].input.text)) { return; }
        this.AddVariable();
    }

    private bool SanitizeInputField(TMP_InputField inputField, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) { inputField.SetTextWithoutNotify("0"); }
        else if (ModelNumbers.TryParse(input, out float value)) { inputField.SetTextWithoutNotify(ModelNumbers.Format(value)); }
        else
        {
            this.ShowError("Enter a finite number.");
            return false;
        }
        return true;
    }

    void ShowError(string message)
    {
        DisplaySingleton.Instance.DisplayText(text => {
            DisplaySingleton.ErrorMsgModification(text);
            text.SetText(message);
        }, 4f, "variable-input-error");
    }

    void Update()
    {
        if (!this.initialized) { return; }
        if (this.focusNameAfterSubmit)
        {
            this.focusNameAfterSubmit = false;
            this.inputs[0].input.Select();
            this.inputs[0].input.ActivateInputField();
        }
        if (!this.inputs.Any(input => input.input.isFocused)) { return; }
        if (Input.GetKeyDown(KeyCode.Tab) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
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

        if (Input.GetKeyDown(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
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

    }

    // TMP consumes Enter and clears focus before our Update can poll it.
    // Its submit event covers both Return and keypad Enter without a global shortcut.
    void SubmitVariable()
    {
        if (!this.initialized || !this.isActiveAndEnabled) { return; }
        for(int i = 0; i < this.inputs.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(this.inputs[i].input.text))
            {
                this.inputs[i].input.SetTextWithoutNotify(UIStrings.DefaultVariableValues[i]);
            }
        }
        this.AddVariable();
        // Let TMP finish deactivating the submitted field before restoring typing focus.
        this.focusNameAfterSubmit = true;
    }

}
