using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class VariablePanel : BasePanel
{
    public const int NUMDISPLAYITEMS = 7;
    
    BaseItemScroll itemScroll;
    BaseScrollbar bScrollbar;

    VariableFields variableFields;

    private string[] myLabels = new string[] { };

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Panel, 0.5f, right,
                new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                    new VirtualProp(PropType.Button, 1f)
                ),
                new VirtualProp(PropType.Scrollbar, -1f, right, right)
            ),
            new VirtualProp(PropType.Image, -1f, typeof(VariableFields))
        );
    }
    void Start()
    {
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>();

        this.itemScroll.SetupItemList(this.SelectVariable, VariablePanel.NUMDISPLAYITEMS, this.myLabels);

        this.bScrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.bScrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.bScrollbar.scrollbar.value = 0f;

        this.variableFields = this.GetComponentInChildren<VariableFields>();

        this.AddListenersToModel();
    }

    public void AddListenersToModel()
    {
        VModel vModel = CommonChip.ClientCore.VirtualModel;
        BaseScrollbar bscb = this.bScrollbar;

        vModel.AddAddedVariableListener(x => this.ReloadVariables());
        vModel.AddAddedVariableListener(x => this.itemScroll.Scroll(bscb.scrollbar.value));

        vModel.AddDeleteVariableListener(x => this.ReloadVariables());
        vModel.AddDeleteVariableListener(x => this.itemScroll.Scroll(bscb.scrollbar.value));

        // The button handle selectin of variables, and since we don't have any buttons in this component, it's not possible
        vModel.AddSetSelectedVariableListener(x => this.itemScroll.Scroll(bscb.scrollbar.value));
    }

    public void SelectVariable(string variableName)
    {
        CommonChip clientCore = CommonChip.ClientCore;
        clientCore.VirtualModel.SetSelectedVariable(variableName);
        VVar v = clientCore.VirtualModel.GetSelectedVariable();

        this.variableFields.PopulateFields(v);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        this.itemScroll.Scroll(0f);
        //print("My labels:");
        //PRINT.IPrint(this.myLabels);
        this.ReloadVariables();
    }

    public void AddVariableLabel(string n)
    {
        this.myLabels = this.myLabels.AddWithoutDuplicate(n);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        this.itemScroll.Scroll(0f);
    }

    public void DeleteVariableLabel(string n)
    {
        this.myLabels = this.myLabels.RemoveElement(n);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        this.itemScroll.Scroll(0f);
    }

    public void ReloadVariables()
    {
        CommonChip clientCore = CommonChip.ClientCore;
        string[] vs = clientCore.VirtualModel.variables.Select(x => x.name).Where(x => x.IsVariableName()).ToArray();
        this.myLabels = vs;
        //print($"itemscroll: {this.itemScroll}, ");
        //print($"virtualcontainer: {this.itemScroll.virtualContainer}, ");
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        this.itemScroll.Scroll(0f);
    }

}

