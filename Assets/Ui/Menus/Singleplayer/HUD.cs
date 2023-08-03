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
        get { return _Focus; }
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

    List<ItemBase> NameItems;
    List<ItemBase> ValueItems;

    List<TMP_Text> variableNames = new List<TMP_Text>();
    List<TMP_Text> variableValues = new List<TMP_Text>();

    void UpdateVariableDisplay()
    {
        var vars = this.variables;
        var numVars = vars.Length;
        var numTexts = variableNames.Count;
        
        if(variableNames.Count != variableValues.Count)
        {
            throw new ArithmeticException($"Number of variable name text boxes ({variableNames.Count}) and variable value text boxes is equal ({variableValues.Count}).");
        }

        for(int i = 0; i < Math.Max(numTexts, numVars); ++i)
        {
            if(i < numVars)
            {
                if (i == variableNames.Count)
                {
                    //variableNames.Add();
                    //variableValues.Add();
                }
                variableNames[i].gameObject.SetActive(true);
                variableNames[i].SetText(vars[i].name);

                variableValues[i].gameObject.SetActive(true);
                variableValues[i].SetText(UIUtils.DisplayFloat(vars[i].defaultValue));
            }
            else
            {
                variableNames[i].gameObject.SetActive(false);
                variableValues[i].gameObject.SetActive(false);
            }
        }
    }

    void Start()
    {
        NameItems = new List<ItemBase>();
        ValueItems = new List<ItemBase>();
        var items = GetComponentsInChildren<ItemBase>();
        foreach(var it in items)
        {
            it.GetComponent<TMP_Text>().fontSize = UIUtils.SmallFontSize;
        }
        NameItems.Add(items[0]);
        ValueItems.Add(items[1]);
    }

    int testInt = 0;
    void Update()
    {
        if(testInt++ < 5) {
            NameItems.Add(NameItems[0].AddItem());
            ValueItems.Add(ValueItems[0].AddItem());
        } else if (testInt == 6)
        {
            StackFrom(NameItems);
            StackFrom(ValueItems);
        }
    }
}
