using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : TopProp
{
    VelocityHUD velocityHUD;

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Panel, 0.1f, up,
                new VirtualProp(PropType.Text, 0.05f, typeof(ItemBaseLeft))
            ),
            new VirtualProp(PropType.Panel, 0.1f, up,
                new VirtualProp(PropType.Text, 0.05f, typeof(ItemBaseRight))
            ),
            new VirtualProp(PropType.Panel, 0.5f),
            new VirtualProp(PropType.Panel, -1f, typeof(VelocityHUD)
            )
        );
    }

    VModel vModel;
    VVar[] variables => this.vModel.variables;

    ItemBase NameItem;
    ItemBase ValueItem;

    ItemBaseLeft[] CurrentNames;
    ItemBaseRight[] CurrentValues;

    TMP_Text[] CurrentNameTxts;
    TMP_Text[] CurrentValueTxts;

    void UpdateVariableDisplay()
    {
        VVar[] vars = this.variables;
        if(vars is null)
        {
            return;
        }

        this.CurrentNames = NameItem.DisplayNItems<ItemBaseLeft>(vars.Length);
        this.CurrentValues = ValueItem.DisplayNItems<ItemBaseRight>(vars.Length);
        this.CurrentNameTxts = this.CurrentNames.Select(x => x.GetComponent<TMP_Text>()).ToArray();
        this.CurrentValueTxts = this.CurrentValues.Select(x => x.GetComponent<TMP_Text>()).ToArray();

        for(int i = 0; i < this.CurrentNames.Length; ++i)
        {
            TMP_Text nameTxt = this.CurrentNames[i].GetComponent<TMP_Text>();
            nameTxt.SetText(vars[i].name);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            TMP_Text valueTxt = this.CurrentValues[i].GetComponent<TMP_Text>();
            valueTxt.SetText(UIUtils.DisplayFloat(vars[i].defaultValue));
            valueTxt.fontSize = UIUtils.SmallFontSize;
        }
        StackFrom(NameItem.Siblings<ItemBaseLeft>(takeInactive: false));
        StackFrom(ValueItem.Siblings<ItemBaseRight>(takeInactive: false));
    }

    void Update()
    {
        for(int i = 0; i < this.variables.Length; ++i)
        {
            this.CurrentValueTxts[i].SetText(this.variables[i].currentValue.ToString());
        }
    }

    void LinkVariables(VModel vModel)
    {
        this.SetupItems();
        this.vModel = vModel;
        UpdateVariableDisplay();
    }
    
    void LinkFocus(CommonChip f)
    {
        this.velocityHUD.SetFocus(f);
    }

    public void LinkCore(CommonChip core)
    {
        this.velocityHUD = this.GetComponentInChildren<VelocityHUD>();

        this.LinkFocus(core);
        this.LinkVariables(core.VirtualModel);
    }

    void SetupItems()
    {
        var items = this.GetComponentsInChildren<ItemBase>();

        foreach(var it in items)
        {
            it.GetComponent<TMP_Text>().fontSize = UIUtils.SmallFontSize;
        }
        this.NameItem = this.GetComponentInChildren<ItemBaseLeft>();
        this.ValueItem = this.GetComponentInChildren<ItemBaseRight>();
    }
}
