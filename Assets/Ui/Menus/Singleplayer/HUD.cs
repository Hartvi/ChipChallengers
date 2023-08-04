using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : TopProp
{
    private CommonChip _Focus;

    public CommonChip Focus
    {
        get
        {
            if (this._Focus == null)
            {
                this._Focus = CommonChip.ClientCore;
            }
            return _Focus;
        }
        set
        {
            if(!value.IsFocusable)
            {
                throw new ArgumentException($"Trying to set focus to a non-focusable chip.");
            }
            _Focus = value;
        }
    }

    Vector3 velocity { get { return _Focus.rb.velocity; } }
    public bool UpdatedModel;
    VirtualVariable[] variables { get { return Focus.AllVirtualVariables; } }

    ItemBase NameItem;
    ItemBase ValueItem;

    List<TMP_Text> variableNames = new List<TMP_Text>();
    List<TMP_Text> variableValues = new List<TMP_Text>();

    void UpdateVariableDisplay()
    {

        var vars = this.variables;
        // test
        //var vars = new VirtualVariable[] { new VirtualVariable(), new VirtualVariable(), new VirtualVariable() };

        var nameItems = NameItem.DisplayNItems(vars.Length);
        var valueItems = ValueItem.DisplayNItems(vars.Length);

        for(int i = 0; i < nameItems.Length; ++i)
        {
            var nameTxt = nameItems[i].GetComponent<TMP_Text>();
            nameTxt.SetText(vars[i].name);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            var valueTxt = valueItems[i].GetComponent<TMP_Text>();
            valueTxt.SetText(UIUtils.DisplayFloat(vars[i].defaultValue));
            valueTxt.fontSize = UIUtils.SmallFontSize;
        }
        StackFrom(NameItem.Siblings<ItemBase>(takeInactive: false));
        StackFrom(ValueItem.Siblings<ItemBase>(takeInactive: false));
    }

    void Start()
    {
        var items = GetComponentsInChildren<ItemBase>();
        foreach(var it in items)
        {
            it.GetComponent<TMP_Text>().fontSize = UIUtils.SmallFontSize;
        }
        NameItem = items[0];
        ValueItem = items[1];
        // TEST
        UpdateVariableDisplay();
    }

    int testInt = 0;
    void Update()
    {
        //if(testInt++ < 5) {
        //    NameItems.Add(NameItems[0].AddItem());
        //    ValueItems.Add(ValueItems[0].AddItem());
        //} else if (testInt == 6)
        //{
        //    StackFrom(NameItems);
        //    StackFrom(ValueItems);
        //}
    }
}
