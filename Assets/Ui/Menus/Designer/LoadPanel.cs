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
        // TODO:
        // load all models from folder models and display them*
        // on click 'ok button': load the corresponding file
        // on click cancel: hide this menu

        this.itemScroll.SetupItemList(LoadModel, Screen.height / 70, IOHelpers.GetAllModels());

        this.btns[0].text.text = "OK";
        this.btns[0].btn.onClick.RemoveAllListeners();
        this.btns[0].btn.onClick.AddListener(this.RebuildWithNewModel);

        this.btns[1].text.text = "CANCEL";
        this.btns[1].btn.onClick.RemoveAllListeners();
        this.btns[1].btn.onClick.AddListener(this.DeactivateLoadPanel);
        this.gameObject.SetActive(false);
    }

    void RebuildWithNewModel()
    {
        if(this.loadedModel == null)
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

    void LoadModel(string modelName)
    {
        //string luaModel = IOHelpers.LoadModel(modelName);
        //print($"Lua model:\n{luaModel}");
        VModel model = null;
        try
        {
            model = VModel.LoadModelFromFile(modelName);
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Model could not be loaded.");
            DisplaySingleton.Instance.DisplayText(this.ModelDoesNotExist, 3f);
        }
        //print($"Model: {model}");
        this.loadedModel = model;
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

    void DeactivateLoadPanel()
    {
        this.gameObject.SetActive(false);
    }

}
