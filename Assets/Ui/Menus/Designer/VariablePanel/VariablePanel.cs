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
        var baseScroll = this.GetComponentInChildren<BaseItemScroll>();

        baseScroll.SetupItemList(this.SelectVariable, VariablePanel.NUMDISPLAYITEMS, this.myLabels);

        var scrollbar = baseScroll.Siblings<BaseScrollbar>(false)[0];

        scrollbar.scrollbar.onValueChanged.AddListener(baseScroll.Scroll);
        baseScroll.Scroll(0f);
        scrollbar.scrollbar.value = 0f;

        this.variableFields = this.GetComponentInChildren<VariableFields>();

        CommonChip.ClientCore.VirtualModel.AddAddedVariableListener(AddVariableLabel);
        CommonChip.ClientCore.VirtualModel.AddAddedVariableListener(x => baseScroll.Scroll(scrollbar.scrollbar.value));

        CommonChip.ClientCore.VirtualModel.AddDeleteVariableListener(DeleteVariableLabel);
        CommonChip.ClientCore.VirtualModel.AddDeleteVariableListener(x => baseScroll.Scroll(scrollbar.scrollbar.value));

        // The button handle selectin of variables, and since we don't have any buttons in this component, it's not possible
        CommonChip.ClientCore.VirtualModel.AddSetSelectedVariableListener(x => baseScroll.Scroll(scrollbar.scrollbar.value));

    }

    public void SelectVariable(string variableName)
    {
        CommonChip clientCore = CommonChip.ClientCore;
        clientCore.VirtualModel.SetSelectedVariable(variableName);
        VVar v = clientCore.VirtualModel.GetSelectedVariable();

        this.variableFields.PopulateFields(v);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
    }

    public void AddVariableLabel(string n)
    {
        this.myLabels = this.myLabels.AddWithoutDuplicate(n);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
    }

    public void DeleteVariableLabel(string n)
    {
        this.myLabels = this.myLabels.RemoveElement(n);
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
    }

}

