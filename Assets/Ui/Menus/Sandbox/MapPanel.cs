using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assimp;

public class MapPanel : BaseScrollMenu
{

    CallbackArray OnLoadedCallbacks = new CallbackArray(false);
    BaseText scenarioWarning;

    // Setup adds a warning above the map list and leaves room for the scenario-exit button label.
    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Image, 1f,
                new VirtualProp(PropType.Text, 0.1f),
                new VirtualProp(PropType.Panel, 0.7f, right,
                    new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                        new VirtualProp(PropType.Button, 1f)),
                    new VirtualProp(PropType.Scrollbar, -1f, right, right)),
                new VirtualProp(PropType.Image, -1f, right,
                    new VirtualProp(PropType.Input, 0.4f),
                    new VirtualProp(PropType.Button, 0.4f),
                    new VirtualProp(PropType.Button, 0.2f))));
    }

    protected override void Start()
    {
        base.Start();
        this.scenarioWarning = this.GetComponentInChildren<BaseText>(true);
        UIUtils.SetFontSize(this.scenarioWarning.text, UIUtils.SmallFontSize);
        this.UpdateScenarioWarning();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllMaps());

        this.btns[0].btn.onClick.AddListener(this.LoadMap);

        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;

        this.input.placeholder.SetText(UIStrings.EnterMapName);
    }

    // LoadMap notifies listeners and closes the panel only after the requested map loads successfully.
    void LoadMap()
    {
        bool wasScenario = GameManager.PlayContext.IsScenario;
        if (!SandboxMenu.myVMap.LoadNewMap(this.input.input.text + UIStrings.MapExtension)) { return; }

        this.DeactivatePanel();

        this.OnLoadedCallbacks.Invoke();
        if (wasScenario && !GameManager.PlayContext.IsScenario)
        {
            DisplaySingleton.Instance.DisplayText(text => {
                DisplaySingleton.BasicSmallModification(text);
                DisplaySingleton.NoOverflowEtc(text);
                var parent = (RectTransform)text.rectTransform.parent;
                text.rectTransform.sizeDelta = new Vector2(parent.rect.width * 0.9f, parent.rect.height * 0.1f);
                text.SetText("Scenario ended — regular sandbox");
            }, 3f, "scenario-map-exit");
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        this.UpdateScenarioWarning();
        if (this.itemScroll is not null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllMaps());
    }

    // UpdateScenarioWarning explains the mode change before a map is loaded from an active scenario.
    void UpdateScenarioWarning()
    {
        if (this.scenarioWarning == null || this.btns == null) { return; }
        bool inScenario = GameManager.PlayContext.IsScenario;
        this.btns[0].text.SetText(inScenario ? "Load map & leave scenario" : "Load map");
        this.scenarioWarning.text.SetText(inScenario
            ? "Loading a map ends the current scenario and returns to regular sandbox."
            : "");
    }

    void FillInput(string modelName)
    {
        this.input.input.SetTextWithoutNotify(modelName);
    }

    public void SetOnLoadedCallbacks(Action[] callbacks)
    {
        this.OnLoadedCallbacks.SetCallbacks(callbacks);
    }

}
