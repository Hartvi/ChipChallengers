using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadPanel : BaseScrollMenu
{
    VModel loadedModel;
    static Action[] OnLoadedCallbacks = { };

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

        this.input.placeholder.SetText(UIStrings.EnterModelName);
        // scroll to models with names starting with entered string
        this.input.input.onValueChanged.AddListener(this.ScrollToName);
    }

    void ScrollToName(string s)
    {
        //print($"scroll: {this.itemScroll}");
        //print($"container: {this.itemScroll.virtualContainer}");
        //print($"container: {this.itemScroll.virtualContainer.items}");
        var items = this.itemScroll.virtualContainer.items;
        int latestIndex = 0;
        for (int i = items.Length - 1; i >= 0; i--)
        {
            var label = items[i].label;
            bool matchingLabel = true;
            for (int k = s.Length - 1; k >= 0; k--)
            {
                if (label.Length >= s.Length)
                {
                    if (label[k] != s[k])
                    {
                        matchingLabel = false;
                        break;
                    }
                }
                else
                {
                    matchingLabel = false;
                    break;
                }
            }
            if (matchingLabel)
            {
                latestIndex = i;
            }
        }
        /*
        0 => 0
        len-displaynum+1 => 1
        y = kx + b
        b = 0
        1 = k (l-d)
         */
        //this.itemScroll.Scroll(Mathf.Min(1f, ((float)(latestIndex)) / ((float)items.Length - this.itemScroll.NumberOfDisplayItems)));
        this.scrollbar.scrollbar.value = Mathf.Min(1f, ((float)(latestIndex)) / ((float)items.Length - this.itemScroll.NumberOfDisplayItems));
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (this.itemScroll is not null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllModels());
    }


    void RebuildWithNewModel()
    {
        VModel model = null;
        string modelNameWithExtension = this.input.input.text + UIStrings.ModelExtension;
        GameManager.Instance.SetModel(this.input.input.text);
#if UNITY_EDITOR
        model = VModel.LoadModelFromFile(modelNameWithExtension);
#else
        try
        {
            model = VModel.LoadModelFromFile(this.input.input.text + UIStrings.ModelExtension);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"Model could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            DisplaySingleton.Instance.DisplayText(LoadPanel.ModelDoesNotExist, 3f);
        }
#endif

        this.loadedModel = model;
        if (this.loadedModel == null)
        {
            // TODO: show error
            DisplaySingleton.Instance.DisplayText(LoadPanel.ModelIsInvalid, 3f);
            return;
        }

        CommonChip core = CommonChip.ClientCore;

        core.TriggerSpawn(this.loadedModel, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));
        HistoryStack.SaveState(core.VirtualModel.ToLuaString());
        this.DeactivatePanel();

        foreach (Action a in LoadPanel.OnLoadedCallbacks)
        {
            a();
        }
    }

    public static void LoadString(string state)
    {
        VModel model = null;
        CommonChip core = CommonChip.ClientCore;
        try
        {
            model = VModel.FromLuaModel(state);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"string model could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            DisplaySingleton.Instance.DisplayText(LoadPanel.UndoRedoNotValid, 3f);

            return;
        }

        core.TriggerSpawn(model, true);
        core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
        core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));
        HistoryStack.SaveState(core.VirtualModel.ToLuaString());

        foreach (Action a in LoadPanel.OnLoadedCallbacks)
        {
            a();
        }
    }

    //public static void LoadTmp()
    //{
    //    VModel model = null;
    //    CommonChip core = CommonChip.ClientCore;
    //    try
    //    {
    //        model = VModel.LoadModelFromFile("tmp");
    //    }
    //    catch (Exception e)
    //    {
    //        UnityEngine.Debug.LogWarning($"`tmp` could not be loaded.");
    //        UnityEngine.Debug.Log(e.Message);
    //        DisplaySingleton.Instance.DisplayText(LoadPanel.UndoRedoNotValid, 3f);

    //        return;
    //    }

    //    core.TriggerSpawn(model, true);
    //    core.VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));
    //    core.VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(core.VirtualModel.ToLuaString()));
    //    HistoryStack.SaveState(core.VirtualModel.ToLuaString());

    //    foreach(Action a in LoadPanel.OnLoadedCallbacks)
    //    {
    //        a();
    //    }
    //}

    void FillInput(string modelName)
    {
        this.input.input.SetTextWithoutNotify(modelName);
    }

    static void ModelDoesNotExist(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Model could not be loaded.");
    }

    static void ModelIsInvalid(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Model is invalid.");
    }
    static void UndoRedoNotValid(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Undo/redo is invalid.");
    }

    public void SetOnLoadedCallbacks(Action[] callbacks)
    {
        LoadPanel.OnLoadedCallbacks = callbacks;
    }

    protected override void SpecificOnStartReceiving()
    {
        this.input.input.SetTextWithoutNotify(GameManager.Instance.GetModel());
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

