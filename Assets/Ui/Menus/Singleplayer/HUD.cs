using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : TopProp
{
    VelocityHUD velocityHUD;

    public override void Setup()
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

    VVar[] displayVariables;

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
        List<VVar> tmpDisplayVariables = new List<VVar>();
        for(int i = 0; i < vars.Length; ++i)
        {
            if (!string.IsNullOrWhiteSpace(vars[i].name))
            {
                tmpDisplayVariables.Add(vars[i]);
            }
        }

        this.displayVariables = tmpDisplayVariables.ToArray();

        this.CurrentNames = NameItem.DisplayNItems<ItemBaseLeft>(this.displayVariables.Length);
        this.CurrentValues = ValueItem.DisplayNItems<ItemBaseRight>(this.displayVariables.Length);
        this.CurrentNameTxts = this.CurrentNames.Select(x => x.GetComponent<TMP_Text>()).ToArray();
        this.CurrentValueTxts = this.CurrentValues.Select(x => x.GetComponent<TMP_Text>()).ToArray();

        for(int i = 0; i < this.CurrentNames.Length; ++i)
        {
            TMP_Text nameTxt = this.CurrentNames[i].GetComponent<TMP_Text>();
            nameTxt.SetText(this.displayVariables[i].name);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            TMP_Text valueTxt = this.CurrentValues[i].GetComponent<TMP_Text>();
            valueTxt.SetText(UIUtils.DisplayFloat(this.displayVariables[i].defaultValue));
            valueTxt.fontSize = UIUtils.SmallFontSize;
        }
        StackFrom(NameItem.Siblings<ItemBaseLeft>(takeInactive: false));
        StackFrom(ValueItem.Siblings<ItemBaseRight>(takeInactive: false));
    }

    void Update()
    {
        for(int i = 0; i < this.displayVariables.Length; ++i)
        {
            // TODO: variable was outside of bounds
            var t = this.displayVariables[i].currentValue.ToString();
            this.CurrentValueTxts[i].SetText(t);
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
        var items = this.GetComponentsInChildren<ItemBase>(true);

        foreach(var it in items)
        {
            it.GetComponent<TMP_Text>().fontSize = UIUtils.SmallFontSize;
        }
        //if(this.NameItem is not null)
        //{
        //    print($"nameitem is active: {this.NameItem.isActiveAndEnabled} or {this.NameItem.gameObject.activeSelf} or {this.NameItem.gameObject.activeInHierarchy}");
        //}
        this.NameItem = this.GetComponentInChildren<ItemBaseLeft>(true);
        this.ValueItem = this.GetComponentInChildren<ItemBaseRight>(true);
    }

}
