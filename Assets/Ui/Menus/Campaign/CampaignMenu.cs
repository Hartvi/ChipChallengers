using System;
using System.Linq;
using UnityEngine;

public class CampaignMenu : BaseMenu, InputReceiver
{
    const int VisibleCampaigns = 8;
    BaseItemScroll campaignList;
    BaseScrollbar scrollbar;
    BaseText selectionText;
    BaseButton playButton;
    string[] campaigns = new string[0];
    string selectedCampaign;

    // Setup declares the campaign list, selection text and navigation layout.
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

    // Start initializes the campaign controls once and connects their actions.
    protected override void Start()
    {
        base.Start();
        this.campaignList = this.GetComponentInChildren<BaseItemScroll>(true);
        this.scrollbar = this.GetComponentInChildren<BaseScrollbar>(true);
        var texts = this.GetComponentsInChildren<BaseText>(true);
        texts[0].text.SetText(UIStrings.Campaign);
        texts[0].text.fontSize = UIUtils.LargeFontSize;
        this.selectionText = texts[1];
        this.selectionText.text.fontSize = UIUtils.SmallFontSize;

        // Start identifies footer buttons before campaignList creates its button copies.
        var buttons = this.GetComponentsInChildren<BaseButton>(true);
        var backButton = buttons[buttons.Length - 2];
        this.playButton = buttons[buttons.Length - 1];
        backButton.text.SetText(UIStrings.Back);
        backButton.text.fontSize = UIUtils.MediumFontSize;
        backButton.btn.onClick.AddListener(this.GoBack);
        this.playButton.text.SetText("Play");
        this.playButton.text.fontSize = UIUtils.MediumFontSize;
        this.playButton.btn.onClick.AddListener(this.PlaySelectedCampaign);

        this.campaignList.SetupItemList(this.SelectCampaign, VisibleCampaigns, this.campaigns);
        this.scrollbar.scrollbar.onValueChanged.AddListener(this.campaignList.Scroll);
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
        this.campaigns = IOHelpers.GetAllScenarios()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        this.campaignList.virtualContainer.UpdateLabels(this.campaigns);
        this.scrollbar.scrollbar.SetValueWithoutNotify(0f);
        this.campaignList.Scroll(0f);
        this.scrollbar.scrollbar.interactable = this.campaigns.Length > VisibleCampaigns;
        if (!this.campaigns.Contains(this.selectedCampaign))
        {
            this.selectedCampaign = this.campaigns.FirstOrDefault();
        }
        this.UpdateSelection();
        UIManager.instance.SwitchToMe(this);
    }

    // SelectCampaign selects a listed scenario and refreshes the selection display.
    void SelectCampaign(string name)
    {
        this.selectedCampaign = name;
        this.UpdateSelection();
    }

    // UpdateSelection shows the selected scenario or empty state and enables Play when available.
    void UpdateSelection()
    {
        this.selectionText.text.SetText(this.selectedCampaign == null
            ? "No campaigns found. Add scenario files to StreamingAssets/Scenarios."
            : "Selected: " + this.selectedCampaign);
        this.playButton.btn.interactable = this.selectedCampaign != null;
    }

    // PlaySelectedCampaign launches the selected scenario through the singleplayer startup flow.
    void PlaySelectedCampaign()
    {
        if (this.selectedCampaign == null) { return; }
        SingleplayerMenu.PlayScenario(this.selectedCampaign);
    }

    // GoBack returns to the main menu.
    void GoBack() => GoToMainMenu.Function(false);

    // IsActive reports whether the campaign menu is active.
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;
    // OnStartReceiving keeps the initialized campaign controls unchanged when input focus begins.
    void InputReceiver.OnStartReceiving() { }
    // OnStopReceiving keeps campaign selection unchanged when input focus ends.
    void InputReceiver.OnStopReceiving() { }
    // HandleInputs handles campaign launch, back navigation and mouse-wheel scrolling.
    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { this.GoBack(); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            this.PlaySelectedCampaign();
            return;
        }
        if (this.campaigns.Length > VisibleCampaigns)
        {
            this.scrollbar.scrollbar.value = Mathf.Clamp01(
                this.scrollbar.scrollbar.value - 0.1f * Input.GetAxis("Mouse ScrollWheel"));
        }
    }
}
