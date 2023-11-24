using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assimp;

public class MapPanel : BaseScrollMenu
{

    Action[] OnLoadedCallbacks = { };

    protected override void Start()
    {
        base.Start();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllMaps());

        this.btns[0].btn.onClick.AddListener(this.LoadMap);

        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;

        this.input.placeholder.SetText(UIStrings.EnterMapName);
    }

    void LoadMap()
    {
        SingleplayerMenu.Instance.myVMap.LoadNewMap(this.input.input.text);

        this.DeactivatePanel();

        foreach(Action a in this.OnLoadedCallbacks)
        {
            a();
        }
    }

    public override void OnEnable()
    {
        if (this.itemScroll is not null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllMaps());
    }

    void FillInput(string modelName)
    {
        this.input.input.SetTextWithoutNotify(modelName);
    }

    public void SetOnLoadedCallbacks(Action[] callbacks)
    {
        this.OnLoadedCallbacks = callbacks;
    }

}
