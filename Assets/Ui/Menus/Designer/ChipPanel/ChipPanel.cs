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
                    new VirtualProp(PropType.Image, 1/3f), 
                    new VirtualProp(PropType.Panel, 1/3f)
                ),
                new VirtualProp(PropType.Panel, 1/3f, right,
                    new VirtualProp(PropType.Image, 1/3f),
                    new VirtualProp(PropType.Panel, 1/3f), 
                    new VirtualProp(PropType.Image, 1/3f)
                ),
                new VirtualProp(PropType.Panel, 1/3f, right,
                    new VirtualProp(PropType.Panel, 1/3f),
                    new VirtualProp(PropType.Image, 1/3f), 
                    new VirtualProp(PropType.Panel, 1/3f)
                )
            ),
            new VirtualProp(PropType.Image, -1f, right,
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

    void DisplayChipProperties(string[] leftTexts, string[] rightTexts)
    {
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

            Debug.LogWarning($"Todo: add callback to update chip values.");
        }

        TopProp.StackFrom(NameItem.Siblings<ItemBase>(takeInactive: false));
        TopProp.StackFrom(ValueItem.Siblings<ItemBase>(takeInactive: false));
    }

    void Start()
    {
        var items = GetComponentsInChildren<ItemBase>();

        NameItem = items[0];
        ValueItem = items[1];

        // TEST
        DisplayChipProperties(VChip.allPropertiesStr, VChip.allPropertiesDefaults);
        // TEST
        DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);
    }

    public void DisplayChip(VChip vc)
    {
        string[] propertiesThisChipHas = ArrayExtensions.AccessLikeDict(vc.instanceProperties[VChip.typeStr], VChip.chipData.keys, VChip.chipData.values);
        //print("properties:");
        //PRINT.print(properties);
    }

}

