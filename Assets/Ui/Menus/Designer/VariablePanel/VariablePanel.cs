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

    // OnEnable restarts model refresh when an initialized variable panel is shown.
    void OnEnable()
    {
        if (this.initialized && this.pendingRefresh == null)
        {
            this.pendingRefresh = StartCoroutine(this.RefreshWhenReady());
        }
    }

    // OnDisable cancels pending refresh and detaches listeners when the panel is hidden.
    protected override void OnDisable()
    {
        base.OnDisable();
        if (this.pendingRefresh != null) { StopCoroutine(this.pendingRefresh); }
        this.pendingRefresh = null;
        this.UnbindModel();
    }

    // OnDestroy detaches model listeners before the variable panel is destroyed.
    void OnDestroy()
    {
        this.UnbindModel();
    }

    // RefreshWhenReady waits for the current player model before binding the variable panel.
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

    // RefreshModel binds listeners to the current model once and reloads its variable list.
    public void RefreshModel()
    {
        // RefreshModel binds only active panels whose controls have initialized.
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

    // AddListenersToModel refreshes the current model binding without adding duplicate listeners.
    public void AddListenersToModel() => this.RefreshModel();

    // UnbindModel removes variable listeners from the previously bound model.
    void UnbindModel()
    {
        if (this.boundModel == null) { return; }
        this.boundModel.RemoveAddedVariableListener(this.OnVariablesChanged);
        this.boundModel.RemoveDeleteVariableListener(this.OnVariablesChanged);
        this.boundModel.RemoveSetSelectedVariableListener(this.OnVariableSelected);
        this.boundModel = null;
    }

    // OnVariablesChanged refreshes the displayed variable list after a variable is added or deleted.
    void OnVariablesChanged(string name)
    {
        this.ReloadVariables();
    }

    // OnVariableSelected populates editable fields for the selected variable and refreshes scrolling.
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

