using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingFields : BasePanel
{

    BaseInput[] baseInputs;
    private Dictionary<string, BaseInput> baseInputsMap = new Dictionary<string, BaseInput>();

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, down,
            new VirtualProp(PropType.Panel, 0.3f, right,
                new VirtualProp(PropType.Panel, 0.5f, down, // column for descriptions of property
                    new VirtualProp(PropType.Text, 0.33f),
                    new VirtualProp(PropType.Text, 0.33f),
                    new VirtualProp(PropType.Text, -1f)
                ),
                new VirtualProp(PropType.Panel, 0.5f, down, // column for input fields for each property
                    new VirtualProp(PropType.Panel, 0.33f, right,
                        new VirtualProp(PropType.Input, 0.5f),
                        new VirtualProp(PropType.Slider, 0.5f)
                    ),
                    new VirtualProp(PropType.Panel, 0.33f, right,
                        new VirtualProp(PropType.Input, 0.5f),
                        new VirtualProp(PropType.Slider, 0.5f)
                    ),
                    new VirtualProp(PropType.Panel, 0.33f, right,
                        new VirtualProp(PropType.Input, 0.5f),
                        new VirtualProp(PropType.Slider, 0.5f)
                    )
                )
            ),
            new VirtualProp(PropType.Panel, 0.1f),
            new VirtualProp(PropType.Button, 0.1f, typeof(GoToMainMenu))
        );
    }

    void Start()
    {
        BaseText[] baseTexts = this.GetComponentsInChildren<BaseText>();
        this.baseInputs = this.GetComponentsInChildren<BaseInput>(); ;
        BaseSlider[] baseSliders = this.GetComponentsInChildren<BaseSlider>(); ;

        for (int i = 0; i < baseTexts.Length; ++i)
        {
            int _i = i;  // temporary _i for callbacks so they actually point to the current base inputs and setting name
            string currentSettingName = UIStrings.SettingsAllProperties[_i];


            baseTexts[i].text.SetText(currentSettingName);
            baseTexts[i].text.fontSize = UIUtils.SmallFontSize;

            this.baseInputs[_i].input.onEndEdit.AddListener(x => OnEndInput(this.baseInputs[_i].input, baseSliders[_i].slider, x, currentSettingName));
            baseSliders[_i].slider.onValueChanged.AddListener(x => this.OnChangeSlider(x, currentSettingName));

            var smf = UIUtils.SmallFontSize;
            this.baseInputs[_i].placeholder.fontSize = smf;
            this.baseInputs[_i].input.textComponent.fontSize = smf;

            this.baseInputs[_i].placeholder.SetText(UIStrings.EnterANumber);

            string prefValStr = this.GetPrefStringValue(currentSettingName);
            var csn = GameManager.minMaxIntSettings[currentSettingName];
            float prefVal = (float)(int.Parse(prefValStr) - csn.Item1) / (csn.Item2 - csn.Item1);
            baseSliders[_i].slider.SetValueWithoutNotify(prefVal);

            this.baseInputs[_i].input.SetTextWithoutNotify(prefValStr);

            this.baseInputsMap[currentSettingName] = this.baseInputs[_i];
        }
    }

    void OnChangeSlider(float val, string settingName)
    {
        if (UIStrings.SettingsIntProperties.Contains(settingName))
        {
            var s = GameManager.minMaxIntSettings[settingName];
            int v = (int)(val * (s.Item2 - s.Item1)) + s.Item1;
            this.SetIntSetting(settingName, v);
            this.baseInputsMap[settingName].input.SetTextWithoutNotify(v.ToString());
        }
        else
        {
            throw new InvalidOperationException($"Non-int settings have not been implemented.");
        }
    }

    void OnEndInput(TMP_InputField inputField, Slider slider, string txt, string settingName)
    {
        string tmp = txt;
        int a;

        while (!int.TryParse(tmp, out a))
        {
            tmp = tmp.Substring(0, tmp.Length - 1);
        }

        if (UIStrings.SettingsIntProperties.Contains(settingName))
        {
            //print($"setting {settingName} to {a}");
            var csn = GameManager.minMaxIntSettings[settingName];
            this.SetIntSetting(settingName, a);
            float prefVal = (float)(this.GetIntSetting(settingName) - csn.Item1) / (csn.Item2 - csn.Item1);
            slider.SetValueWithoutNotify(prefVal);
        }
        else
        {
            throw new InvalidOperationException($"Non-int settings have not been implemented.");
        }

        inputField.SetTextWithoutNotify(this.GetIntSetting(settingName).ToString());
    }

    string GetPrefStringValue(string settingName)
    {
        if (UIStrings.SettingsIntProperties.Contains(settingName))
        {
            return PlayerPrefs.GetInt(settingName).ToString();
        }
        else
        {
            throw new InvalidOperationException($"Non-int settings have not been implemented yet.");
        }
    }

    int GetIntSetting(string settingName)
    {
        return GameManager.Instance.GettingUpdateFunctions[settingName]();
    }

    void SetIntSetting(string settingName, int settingValue)
    {
        if (settingValue != PlayerPrefs.GetInt(settingName))
        {
            PlayerPrefs.SetInt(settingName, settingValue);
            PlayerPrefs.Save();
            GameManager.Instance.UpdateSettings();
        }
    }
}

