using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadPanel : BaseScrollMenu
{
    VModel loadedModel;
    protected override void Start()
    {
        base.Start();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllModels());

        this.btns[0].btn.onClick.AddListener(this.RebuildWithNewModel);

        this.btns[1].btn.onClick.AddListener(this.DeactivateLoadPanel);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;

        this.input.placeholder.SetText("Enter model name");
    }

    public override void OnEnable()
    {
        if (this.itemScroll is not null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllModels());
    }


    void RebuildWithNewModel()
    {
        VModel model = null;
        try
        {
            model = VModel.LoadModelFromFile(this.input.input.text);
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Model could not be loaded.");
            DisplaySingleton.Instance.DisplayText(this.ModelDoesNotExist, 3f);
        }

        this.loadedModel = model;
        if (this.loadedModel == null)
        {
            // TODO: show error
            DisplaySingleton.Instance.DisplayText(this.ModelIsInvalid, 3f);
            return;
        }

        CommonChip core = CommonChip.ClientCore;

        //print($"Chips: {core.VirtualModel.chips.Length}");

        //print($"loaded model core: {this.loadedModel.Core.Children}");
        core.TriggerSpawn(this.loadedModel, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        //print($"Chips: {core.VirtualModel.chips.Length}");
        this.DeactivateLoadPanel();
    }

    void FillInput(string modelName)
    {
        this.input.input.SetTextWithoutNotify(modelName);
    }

    void ModelDoesNotExist(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Model could not be loaded.");
    }

    void ModelIsInvalid(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Model is invalid.");
    }
}

