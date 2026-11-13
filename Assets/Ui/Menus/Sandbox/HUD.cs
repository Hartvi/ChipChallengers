using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : TopProp
{
    VelocityHUD velocityHUD;
    CanvasGroup visibility;

    public override void Setup()
    {
        base.Setup();
        this.visibility = this.GetComponent<CanvasGroup>();
        if (this.visibility == null)
        {
            this.visibility = this.gameObject.AddComponent<CanvasGroup>();
        }
        this.visibility.alpha = 0f;
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Panel, 0.05f),
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

    int[] displayVariables;

    ItemBase NameItem;
    ItemBase ValueItem;

    ItemBaseLeft[] CurrentNames;
    ItemBaseRight[] CurrentValues;

    TMP_Text[] CurrentNameTxts;
    TMP_Text[] CurrentValueTxts;

    void UpdateVariableDisplay()
    {
        VVar[] vars = this.variables;
        if (vars is null)
        {
            Debug.LogError("VARIABLES ARE NULL IN HUD");
            return;
        }
        List<int> tmpDisplayVariables = new List<int>();
        for (int i = 0; i < vars.Length; ++i)
        {
            if (!string.IsNullOrWhiteSpace(vars[i].name))
            {
                tmpDisplayVariables.Add(i);
            }
        }

        this.displayVariables = tmpDisplayVariables.ToArray();

        this.CurrentNames = NameItem.DisplayNItems<ItemBaseLeft>(this.displayVariables.Length);
        this.CurrentValues = ValueItem.DisplayNItems<ItemBaseRight>(this.displayVariables.Length);
        this.CurrentNameTxts = this.CurrentNames.Select(x => x.GetComponent<TMP_Text>()).ToArray();
        this.CurrentValueTxts = this.CurrentValues.Select(x => x.GetComponent<TMP_Text>()).ToArray();

        for (int i = 0; i < this.CurrentNames.Length; ++i)
        {
            TMP_Text nameTxt = this.CurrentNames[i].GetComponent<TMP_Text>();
            nameTxt.SetText(this.variables[this.displayVariables[i]].name);
            UIUtils.SetFontSize(nameTxt, UIUtils.SmallFontSize);

            TMP_Text valueTxt = this.CurrentValues[i].GetComponent<TMP_Text>();
            valueTxt.SetText(UIUtils.DisplayFloat(this.variables[this.displayVariables[i]].defaultValue));
            UIUtils.SetFontSize(valueTxt, UIUtils.SmallFontSize);
        }
        StackFrom(NameItem.Siblings<ItemBaseLeft>(takeInactive: false));
        StackFrom(ValueItem.Siblings<ItemBaseRight>(takeInactive: false));
    }

    void Update()
    {
        if (this.displayVariables == null)
        {
            return;
        }
        for (int i = 0; i < this.displayVariables.Length; ++i)
        {
            // TODO: variable was outside of bounds
            var t = this.variables[this.displayVariables[i]].currentValue.ToString();
            if (this.CurrentValues.Length == i) { break; }
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

    public void LinkCore(CoreChip core)
    {
        this.velocityHUD = this.GetComponentInChildren<VelocityHUD>();

        this.LinkFocus(core);
        this.LinkVariables(core.VirtualModel);
        this.visibility.alpha = 1f;
    }

    void SetupItems()
    {
        var items = this.GetComponentsInChildren<ItemBase>(true);

        foreach (var it in items)
        {
            UIUtils.SetFontSize(it.GetComponent<TMP_Text>(), UIUtils.SmallFontSize);
        }
        //if(this.NameItem is not null)
        //{
        //    print($"nameitem is active: {this.NameItem.isActiveAndEnabled} or {this.NameItem.gameObject.activeSelf} or {this.NameItem.gameObject.activeInHierarchy}");
        //}
        this.NameItem = this.GetComponentInChildren<ItemBaseLeft>(true);
        this.ValueItem = this.GetComponentInChildren<ItemBaseRight>(true);
    }

}
