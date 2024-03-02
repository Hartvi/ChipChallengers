using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SettingFields : BasePanel
{

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
                    new VirtualProp(PropType.Input, 0.33f),
                    new VirtualProp(PropType.Input, 0.33f),
                    new VirtualProp(PropType.Input, -1f)
                )
            ),
            new VirtualProp(PropType.Panel, 0.1f),
            new VirtualProp(PropType.Button, 0.1f, typeof(GoToMainMenu))
        );
    }

    void Start()
    {
        BaseText[] baseTexts = this.GetComponentsInChildren<BaseText>();
        BaseInput[] baseInputs = this.GetComponentsInChildren<BaseInput>(); ;

        for (int i = 0; i < baseTexts.Length; ++i)
        {
            int _i = i;  // temporary _i for callbacks so they actually point to the current base inputs and setting name
            string currentSettingName = UIStrings.SettingsAllProperties[_i];


            baseTexts[i].text.SetText(currentSettingName);
            baseTexts[i].text.fontSize = UIUtils.SmallFontSize;

            baseInputs[_i].input.onEndEdit.AddListener(x => OnEndInput(baseInputs[_i].input, x, currentSettingName));

            var smf = UIUtils.SmallFontSize;
            baseInputs[_i].placeholder.fontSize = smf;
            baseInputs[_i].input.textComponent.fontSize = smf;

            baseInputs[_i].placeholder.SetText(UIStrings.EnterANumber);

            baseInputs[i].input.SetTextWithoutNotify(this.GetPrefStringValue(currentSettingName));
        }
    }

    void OnEndInput(TMP_InputField inputField, string txt, string settingName)
    {
        string tmp = txt;
        int a;

        while(!int.TryParse(tmp, out a)) {
            tmp = tmp.Substring(0, tmp.Length - 1);
        }

        //a = GameManager.Instance.SaturateIntSetting(settingName, a);
        

        if (UIStrings.SettingsIntProperties.Contains(settingName))
        {
            //print($"setting {settingName} to {a}");
            this.SetIntSetting(settingName, a);
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

    int GetIntSetting(string settingName) {
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

