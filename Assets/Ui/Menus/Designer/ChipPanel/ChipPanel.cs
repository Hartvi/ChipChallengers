using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChipPanel : BasePanel
{

    void Start()
    {
        var items = GetComponentsInChildren<ItemBase>();

        NameItem = items[0];
        ValueItem = items[1];

        // TEST
        //DisplayChipProperties(VChip.allPropertiesStr, VChip.allPropertiesDefaults);
        // TEST
        //DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);

        // Link highlighting in the editor in general to displaying in this menu
        EditorMenu em = this.GetComponentInParent<EditorMenu>();
        em.highlighter.SetHighlightCallbacks(new Action<VChip>[] { this.DisplayChip });
        // add default values so it renders fine at the start
        //print("STARTING CHIP PANEL");
        this.DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);
    }

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Panel, 0.5f, down,
                new VirtualProp(PropType.Panel, 1/3f, right,
                    new VirtualProp(PropType.Panel, 1/3f),
                    new VirtualProp(PropType.Panel, 1/3f), 
                    new VirtualProp(PropType.Panel, 1/3f)
                ),
                new VirtualProp(PropType.Panel, 1/3f, right,
                    new VirtualProp(PropType.Panel, 1/3f),
                    new VirtualProp(PropType.Panel, 1/3f), 
                    new VirtualProp(PropType.Panel, 1/3f)
                ),
                new VirtualProp(PropType.Panel, 1/3f, right,
                    new VirtualProp(PropType.Panel, 1/3f),
                    new VirtualProp(PropType.Panel, 1/3f), 
                    new VirtualProp(PropType.Panel, 1/3f)
                )
            ),
            new VirtualProp(PropType.Panel, -1f, right,
                new VirtualProp(PropType.Panel, 0.5f, up,
                    new VirtualProp(PropType.Text, 1f/(float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                ),
                new VirtualProp(PropType.Panel, 0.5f, up,
                    new VirtualProp(PropType.Input, 1f/(float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                )
            )
        );
    }

    ItemBase NameItem;
    ItemBase ValueItem;

    (TMP_Text[], TMP_InputField[]) DisplayChipProperties(string[] leftTexts, string[] rightTexts)
    {
        if(leftTexts.Length != rightTexts.Length)
        {
            throw new ArgumentException($"Left texts {leftTexts.Length} aren't the same length as right texts {rightTexts.Length}.");
        }

        TMP_InputField[] inputs = new TMP_InputField[rightTexts.Length];
        TMP_Text[] texts = new TMP_Text[rightTexts.Length];

        ItemBase[] nameItems = NameItem.DisplayNItems(leftTexts.Length);
        ItemBase[] valueItems = ValueItem.DisplayNItems(rightTexts.Length);

        int offset = nameItems.Length - 1;
        for(int i = 0; i < nameItems.Length; ++i)
        {
            int reverseIndex = offset - i;
            var nameTxt = nameItems[i].GetComponent<TMP_Text>();
            nameTxt.SetText(leftTexts[i]);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            texts[i] = nameTxt;

            var valueTxt = valueItems[i].GetComponent<TMP_InputField>();
            valueTxt.SetTextWithoutNotify(rightTexts[i]);
            valueTxt.textComponent.fontSize = UIUtils.SmallFontSize;

            inputs[i] = valueTxt;

            //Debug.LogWarning($"Todo: add callback to update chip values.");
        }

        TopProp.StackFrom(NameItem.Siblings<ItemBase>(takeInactive: false));
        TopProp.StackFrom(ValueItem.Siblings<ItemBase>(takeInactive: false));

        return (texts, inputs);
    }

    public void DisplayChip(VChip vc)
    {
        //print($"DisplayChip: Chip type: {vc.ChipType}");
        string[] newKeys = ArrayExtensions.AccessLikeDict(vc.ChipType, VChip.chipData.keys, VChip.chipData.values);
        string[] newValues = new string[newKeys.Length];

        for(int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            //print($"current property: {currentProperty}");
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if(val is not null)
            {
                newValues[i] = val.ToString();
            }
            else
            {
                newValues[i] = ArrayExtensions.AccessLikeDict(currentProperty, VChip.allPropertiesStr, VChip.allPropertiesDefaultsStrings);
                //throw new ArgumentNullException($"Property {propertiesThisChipHas[i]} doesn't exist in chip {vc} of type {vc.ChipType}.");
            }
        }

        // This is so we can set it below using callbacks that index assuming the full length of properties
        vc.vals = newValues;
        vc.keys = newKeys;

        //PRINT.print(newKeys);
        //PRINT.print(newValues);

        (TMP_Text[] texts, TMP_InputField[] inputs) = this.DisplayChipProperties(newKeys, newValues);

        for(int i = 0; i < inputs.Length; ++i)
        {
            int _i = i;

            inputs[_i].onEndEdit.RemoveAllListeners();
            inputs[_i].onEndEdit.AddListener(
                x => {
                    //print($"chip type in callback: {vc.ChipType}, text: {texts[_i].text}, val: {x}");
                    string validityMsg = vc.CheckValidityOfPropertyForThisChip(texts[_i].text, x);
                    if(validityMsg is not null)
                    {
                        inputs[_i].SetTextWithoutNotify(vc.vals[_i]);
                        DisplaySingleton.Instance.DisplayText(ChipPanel.SetFormatMsg(validityMsg), 3f);
                    }
                    else
                    {
                        //print($"Changing val {vc.vals[_i]} to {inputs[_i].text}");
                        // TODO CALL BACK FOR CHIP CHANGED TO PROPAGATE TO MODEL CHANGED
                        Debug.LogWarning($"TODO: make chip changed callback propagate to model changed callback.");
                        vc.vals[_i] = x;
                    }
                }
            );
        }
    }

    static Action<TMP_Text> SetFormatMsg(string msg)
    {
        Action<TMP_Text> action = x => 
        {
            x.SetText(msg);
            DisplaySingleton.ErrorMsgModification(x);
        };
        return action;
    }
    
}

