using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadPanel : BaseScrollMenu
{
    VModel loadedModel;
    Action[] OnLoadedCallbacks = { };

    protected override void Start()
    {
        base.Start();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllModels());

        this.btns[0].btn.onClick.AddListener(this.RebuildWithNewModel);

        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);

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
#if UNITY_EDITOR
        model = VModel.LoadModelFromFile(this.input.input.text);
#else
        try
        {
            model = VModel.LoadModelFromFile(this.input.input.text);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"Model could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            //DisplaySingleton.Instance.DisplayText(this.ModelDoesNotExist, 3f);
        }
#endif

        this.loadedModel = model;
        if (this.loadedModel == null)
        {
            // TODO: show error
            DisplaySingleton.Instance.DisplayText(this.ModelIsInvalid, 3f);
            return;
        }

        CommonChip core = CommonChip.ClientCore;

        core.TriggerSpawn(this.loadedModel, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));

        this.DeactivatePanel();

        foreach(Action a in this.OnLoadedCallbacks)
        {
            a();
        }
    }

    public void LoadString(string state)
    {
        VModel model = null;
        CommonChip core = CommonChip.ClientCore;
        try
        {
            model = VModel.FromLuaModel(state);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"`tmp` could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            DisplaySingleton.Instance.DisplayText(this.UndoRedoNotValid, 3f);

            return;
        }

        core.TriggerSpawn(model, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));

        foreach(Action a in this.OnLoadedCallbacks)
        {
            a();
        }
    }

    public void LoadTmp()
    {
        VModel model = null;
        CommonChip core = CommonChip.ClientCore;
        try
        {
            model = VModel.LoadModelFromFile("tmp");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"`tmp` could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            DisplaySingleton.Instance.DisplayText(this.UndoRedoNotValid, 3f);

            return;
        }

        core.TriggerSpawn(model, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));

        foreach(Action a in this.OnLoadedCallbacks)
        {
            a();
        }
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
    void UndoRedoNotValid(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Undo/redo is invalid.");
    }

    public void SetOnLoadedCallbacks(Action[] callbacks)
    {
        this.OnLoadedCallbacks = callbacks;
    }

    /*
    {
        chips = {
            {id = 'a', orientation = '0', parentId = '', Colour = '#FFFFFF', Name = 'joe', Type = 'Core', }, 
            { id = 'aa', orientation = '0', parentId = 'a', Angle = '20', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '1', Name = 'joe', Type = 'Chip', }, 
            { id = 'ab', orientation = '1', parentId = 'a', Angle = '20', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aaa', orientation = '0', parentId = 'aa', Angle = '20', Colour = '#FFFFFF', Spring = '1e9f', Damper = '1e6f', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aaaa', orientation = '0', parentId = 'aaa', Angle = 'asd', Colour = '#FFFFFF', Spring = '10', Damper = '10', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aaab', orientation = '1', parentId = 'aaa', Angle = '20', Colour = '#FFFFFF', Spring = '10', Damper = '10', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aaac', orientation = '2', parentId = 'aaa', Angle = '20', Colour = '#FFFFFF', Spring = '10', Damper = '10', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aaad', orientation = '3', parentId = 'aaa', Angle = '20', Colour = '#FFFFFF', Spring = '10', Damper = '10', Option = '1', Name = 'joe', Type = 'Chip', },
            { id = 'aab', orientation = '3', parentId = 'aa', Angle = '0', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '0', Name = 'chip_name', Type = 'Chip', },
            { id = 'aac', orientation = '1', parentId = 'aa', Angle = '0', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '0', Name = 'chip_name', Type = 'Chip', },
        },
        variables = {
            { name = '', defaultValue = 0, maxValue = 1, minValue = 0, backstep = 1, },
            { name = 'asd', defaultValue = 0, maxValue = 20, minValue = 0, backstep = 1, },
        },
        script = 'SetVar("asd", 20);'
    }
    //*/
    /*
    {
        chips = {
            {id = 'a', orientation = '0', parentId = '', Colour = '#FFFFFF', Name = 'joe', Type = 'Core', }, 
            {id = 'aa', orientation = '0', parentId = 'a', Angle = '20', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '1', Name = 'joe', Type = 'Chip', }, 
            {id = 'ab', orientation = '1', parentId = 'a', Angle = '20', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '1', Name = 'joe', Type = 'Chip', }, 
            {id = 'aaa', orientation = '0', parentId = 'aa', Name = 'joe', Angle = '20', Value = '5', Option = '1', Type = 'Chip', }, 
            {id = 'aaaa', orientation = '0', parentId = 'aaa', Name = 'joe', Angle = '20', Value = '5', Option = '1', Type = 'Chip', Spring = '10', Damper = '10', }, 
            {id = 'aaab', orientation = '1', parentId = 'aaa', Name = 'joe', Angle = '20', Value = '5', Option = '1', Type = 'Chip', Spring = '10', Damper = '10', }, 
            {id = 'aaac', orientation = '2', parentId = 'aaa', Name = 'joe', Angle = '20', Value = '5', Option = '1', Type = 'Chip', Spring = '10', Damper = '10', }, 
            {id = 'aaad', orientation = '3', parentId = 'aaa', Name = 'joe', Angle = '20', Value = '5', Option = '1', Type = 'Chip', Spring = '10', Damper = '10', }, 
            {id = 'aab', orientation = '3', parentId = 'aa', Angle = '0', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '0', Name = 'chip_name', Type = 'Chip', }, 
            {id = 'aac', orientation = '1', parentId = 'aa', Angle = '0', Colour = '#FFFFFF', Spring = '-1', Damper = '-1', Option = '0', Name = 'chip_name', Type = 'Chip', }, 
        }, 
        variables = {
            {name = '', defaultValue = 0, maxValue = 1, minValue = -1, backstep = 1, }, 
        }, 
    }
     //*/
}

