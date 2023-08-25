using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChipPanel : BasePanel
{

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

    TMP_InputField[] DisplayChipProperties(string[] leftTexts, string[] rightTexts)
    {
        if(leftTexts.Length != rightTexts.Length)
        {
            throw new ArgumentException($"Left texts {leftTexts.Length} aren't the same length as right texts {rightTexts.Length}.");
        }

        TMP_InputField[] inputs = new TMP_InputField[rightTexts.Length];

        ItemBase[] nameItems = NameItem.DisplayNItems(leftTexts.Length);
        ItemBase[] valueItems = ValueItem.DisplayNItems(rightTexts.Length);

        int offset = nameItems.Length - 1;
        for(int i = 0; i < nameItems.Length; ++i)
        {
            int reverseIndex = offset - i;
            var nameTxt = nameItems[i].GetComponent<TMP_Text>();
            nameTxt.SetText(leftTexts[reverseIndex]);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            var valueTxt = valueItems[i].GetComponent<TMP_InputField>();
            valueTxt.SetTextWithoutNotify(rightTexts[reverseIndex]);
            valueTxt.textComponent.fontSize = UIUtils.SmallFontSize;

            inputs[i] = valueTxt;

            Debug.LogWarning($"Todo: add callback to update chip values.");
        }

        TopProp.StackFrom(NameItem.Siblings<ItemBase>(takeInactive: false));
        TopProp.StackFrom(ValueItem.Siblings<ItemBase>(takeInactive: false));

        return inputs;
    }

    void Start()
    {
        var items = GetComponentsInChildren<ItemBase>();

        NameItem = items[0];
        ValueItem = items[1];

        // TEST
        //DisplayChipProperties(VChip.allPropertiesStr, VChip.allPropertiesDefaults);
        // TEST
        //DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);
        EditorMenu em = this.GetComponentInParent<EditorMenu>();
        em.AddHighlightCallback(this.DisplayChip);
    }

    public void DisplayChip(VChip vc)
    {
        string[] propertiesThisChipHas = ArrayExtensions.AccessLikeDict(vc.instanceProperties[VChip.typeStr], VChip.chipData.keys, VChip.chipData.values);
        string[] propertyValues = new string[propertiesThisChipHas.Length];

        for(int i = 0; i < propertiesThisChipHas.Length; ++i)
        {
            object val;
            if(vc.instanceProperties.TryGetValue(propertiesThisChipHas[i], out val))
            {
                propertyValues[i] = val.ToString();
            }
            else
            {
                propertyValues[i] = ArrayExtensions.AccessLikeDict(propertiesThisChipHas[i], VChip.allPropertiesStr, VChip.allPropertiesDefaults);
                //throw new ArgumentNullException($"Property {propertiesThisChipHas[i]} doesn't exist in chip {vc} of type {vc.ChipType}.");
            }
        }
        TMP_InputField[] inputs = DisplayChipProperties(propertiesThisChipHas, propertyValues);

        //print("properties:");
        //PRINT.print(properties);
    }

}

