using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Mirror;

public class VariablePanel : BaseSidePanel, InputReceiver
{
    public const int NUMDISPLAYITEMS = 7;
    
    BaseItemScroll itemScroll;
    BaseScrollbar bScrollbar;

    VariableFields variableFields;

    private string[] myLabels = new string[] { };

    BaseImage backgroundImage;
    bool initialized;
    Coroutine pendingRefresh;
    VModel boundModel;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Image, 1f,
                new VirtualProp(PropType.Panel, 0.5f, right,
                    new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                        new VirtualProp(PropType.Button, 1f)
                    ),
                    new VirtualProp(PropType.Scrollbar, -1f, right, right)
                ),
                new VirtualProp(PropType.Image, -1f, typeof(VariableFields))
            )
        );
    }

    void Start()
    {
        this.backgroundImage = this.GetComponentInChildren<BaseImage>(true);
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>(true);
        this.itemScroll.SetupItemList(this.SelectVariable, NUMDISPLAYITEMS, this.myLabels);
        this.bScrollbar = this.GetComponentInChildren<BaseScrollbar>(true);
        this.bScrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.bScrollbar.scrollbar.SetValueWithoutNotify(0f);
        this.variableFields = this.GetComponentInChildren<VariableFields>(true);
        this.initialized = true;
        this.pendingRefresh = StartCoroutine(this.RefreshWhenReady());
    }

    void OnEnable()
    {
        if (this.initialized && this.pendingRefresh == null)
        {
            this.pendingRefresh = StartCoroutine(this.RefreshWhenReady());
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (this.pendingRefresh != null) { StopCoroutine(this.pendingRefresh); }
        this.pendingRefresh = null;
        this.UnbindModel();
    }

    void OnDestroy()
    {
        this.UnbindModel();
    }

    IEnumerator RefreshWhenReady()
    {
        yield return null;
        while (NetworkClient.localPlayer == null ||
               CoreChip.ClientCoreChip.IsRebuilding ||
               CoreChip.ClientCoreChip.loadedModelString == null)
        {
            yield return null;
        }
        this.RefreshModel();
        this.pendingRefresh = null;
    }

    public void RefreshModel()
    {
        // Inactive panels bind when next shown, after their controls have started.
        if (!this.initialized || !this.isActiveAndEnabled ||
            NetworkClient.localPlayer == null) { return; }
        var model = CoreChip.ClientCoreChip.VirtualModel;
        if (!ReferenceEquals(this.boundModel, model))
        {
            this.UnbindModel();
            this.boundModel = model;
            model.AddAddedVariableListener(this.OnVariablesChanged);
            model.AddDeleteVariableListener(this.OnVariablesChanged);
            model.AddSetSelectedVariableListener(this.OnVariableSelected);
        }
        this.ReloadVariables();
    }

    // Retained for existing callers; binding is idempotent for a given model.
    public void AddListenersToModel() => this.RefreshModel();

    void UnbindModel()
    {
        if (this.boundModel == null) { return; }
        this.boundModel.RemoveAddedVariableListener(this.OnVariablesChanged);
        this.boundModel.RemoveDeleteVariableListener(this.OnVariablesChanged);
        this.boundModel.RemoveSetSelectedVariableListener(this.OnVariableSelected);
        this.boundModel = null;
    }

    void OnVariablesChanged(string name)
    {
        this.ReloadVariables();
    }

    void OnVariableSelected(string name)
    {
        this.variableFields.PopulateFields(this.boundModel.GetSelectedVariable());
        this.itemScroll.Scroll(this.bScrollbar.scrollbar.value);
    }

    public void SelectVariable(string variableName)
    {
        var clientCore = CoreChip.ClientCoreChip;
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
        if (!this.initialized || NetworkClient.localPlayer == null) { return; }
        var clientCore = CoreChip.ClientCoreChip;
        string[] vs = clientCore.VirtualModel.variables.Select(x => x.name).Where(x => x.IsVariableName()).ToArray();
        this.myLabels = vs;
        //print($"itemscroll: {this.itemScroll}, ");
        //print($"virtualcontainer: {this.itemScroll.virtualContainer}, ");
        this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        this.itemScroll.Scroll(0f);
    }

    void Update()
    {
        if (this.initialized && NetworkClient.localPlayer != null)
        {
            var core = CoreChip.ClientCoreChip;
            if (!core.IsRebuilding && core.loadedModelString != null &&
                !ReferenceEquals(this.boundModel, core.VirtualModel))
            {
                this.RefreshModel();
            }
        }


        if (Input.GetMouseButtonDown(0))
        {
            if (this.insideChipPanel)
            {
                UIManager.instance.SwitchToMe(this);
            }
        }
    }

    void InputReceiver.HandleInputs()
    {
        if (!this.insideChipPanel)
        {
            UIManager.instance.TurnMeOff(this);
        }
    }

    void InputReceiver.OnStopReceiving()
    {
    }

    void InputReceiver.OnStartReceiving()
    {
    }
    bool InputReceiver.IsActive()
    {
        return this.gameObject.activeSelf;
    }
}

