using System;
using System.Linq;
using UnityEngine;

public class ScenarioMenu : BaseMenu, InputReceiver
{
    const int VisibleScenarios = 8;
    BaseItemScroll scenarioList;
    BaseScrollbar scrollbar;
    BaseText selectionText;
    BaseButton playButton;
    string[] scenarios = new string[0];
    string selectedScenario;

    // Setup declares the scenario list, selection text and navigation layout.
    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, down,
            new VirtualProp(PropType.Image, 1f, down,
                new VirtualProp(PropType.Text, 0.12f),
                new VirtualProp(PropType.Panel, 0.72f, right,
                    new VirtualProp(PropType.Panel, 0.96f, typeof(BaseItemScroll),
                        new VirtualProp(PropType.Button, 1f)
                    ),
                    new VirtualProp(PropType.Scrollbar, -1f, right, right)
                ),
                new VirtualProp(PropType.Text, 0.08f),
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Button, 0.5f),
                    new VirtualProp(PropType.Button, -1f)
                )
            )
        );
    }

    // Start initializes the scenario controls once and connects their actions.
    protected override void Start()
    {
        base.Start();
        this.scenarioList = this.GetComponentInChildren<BaseItemScroll>(true);
        this.scrollbar = this.GetComponentInChildren<BaseScrollbar>(true);
        var texts = this.GetComponentsInChildren<BaseText>(true);
        texts[0].text.SetText(UIStrings.Scenarios);
        texts[0].text.fontSize = UIUtils.LargeFontSize;
        this.selectionText = texts[1];
        this.selectionText.text.fontSize = UIUtils.SmallFontSize;

        // Start identifies footer buttons before scenarioList creates its button copies.
        var buttons = this.GetComponentsInChildren<BaseButton>(true);
        var backButton = buttons[buttons.Length - 2];
        this.playButton = buttons[buttons.Length - 1];
        backButton.text.SetText(UIStrings.Back);
        backButton.text.fontSize = UIUtils.MediumFontSize;
        backButton.btn.onClick.AddListener(this.GoBack);
        this.playButton.text.SetText("Play");
        this.playButton.text.fontSize = UIUtils.MediumFontSize;
        this.playButton.btn.onClick.AddListener(this.PlaySelectedScenario);

        this.scenarioList.SetupItemList(this.SelectScenario, VisibleScenarios, this.scenarios);
        this.scrollbar.scrollbar.onValueChanged.AddListener(this.scenarioList.Scroll);
        this.selectedCallbacks.SetCallbacks(new Action[] { this.OnEnterMenu });
        this.deselectedCallbacks.SetCallbacks(new Action[] {
            () => UIManager.instance.TurnMeOff(this)
        });
        this.OnEnterMenu();
    }

    // OnEnterMenu refreshes available scenario names and restores selection without starting a session.
    void OnEnterMenu()
    {
        // OnEnterMenu discovers scenario files through IOHelpers.GetAllScenarios without loading gameplay.
        this.scenarios = IOHelpers.GetAllScenarios()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        this.scenarioList.virtualContainer.UpdateLabels(this.scenarios);
        this.scrollbar.scrollbar.SetValueWithoutNotify(0f);
        this.scenarioList.Scroll(0f);
        this.scrollbar.scrollbar.interactable = this.scenarios.Length > VisibleScenarios;
        if (!this.scenarios.Contains(this.selectedScenario))
        {
            this.selectedScenario = this.scenarios.FirstOrDefault();
        }
        this.UpdateSelection();
        UIManager.instance.SwitchToMe(this);
    }

    // SelectScenario selects a listed scenario and refreshes the selection display.
    void SelectScenario(string name)
    {
        this.selectedScenario = name;
        this.UpdateSelection();
    }

    // UpdateSelection shows the selected scenario or empty state and enables Play when available.
    void UpdateSelection()
    {
        this.selectionText.text.SetText(this.selectedScenario == null
            ? "No scenarios found. Add scenario files to StreamingAssets/Scenarios."
            : "Selected: " + this.selectedScenario);
        this.playButton.btn.interactable = this.selectedScenario != null;
    }

    // PlaySelectedScenario launches the selected scenario through the singleplayer startup flow.
    void PlaySelectedScenario()
    {
        if (this.selectedScenario == null) { return; }
        SingleplayerMenu.PlayScenario(this.selectedScenario);
    }

    // GoBack returns to the main menu.
    void GoBack() => GoToMainMenu.Function(false);

    // IsActive reports whether the scenario menu is active.
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;
    // OnStartReceiving keeps the initialized scenario controls unchanged when input focus begins.
    void InputReceiver.OnStartReceiving() { }
    // OnStopReceiving keeps scenario selection unchanged when input focus ends.
    void InputReceiver.OnStopReceiving() { }
    // HandleInputs handles scenario launch, back navigation and mouse-wheel scrolling.
    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { this.GoBack(); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            this.PlaySelectedScenario();
            return;
        }
        if (this.scenarios.Length > VisibleScenarios)
        {
            this.scrollbar.scrollbar.value = Mathf.Clamp01(
                this.scrollbar.scrollbar.value - 0.1f * Input.GetAxis("Mouse ScrollWheel"));
        }
    }
}
