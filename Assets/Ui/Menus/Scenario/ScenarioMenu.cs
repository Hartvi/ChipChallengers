using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class ChallengeMenu : BaseMenu, InputReceiver
{
    const int VisibleChallenges = 8;
    BaseItemScroll challengeList;
    BaseScrollbar scrollbar;
    BaseText selectionText;
    BaseButton playButton;
    string[] challenges = new string[0];
    string selectedChallenge;
    readonly Dictionary<string, ChallengeRunRecord> bestRuns = new Dictionary<string, ChallengeRunRecord>();

    // Setup declares the challenge list, selection text and navigation layout.
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

    // Start initializes the challenge controls once and connects their actions.
    protected override void Start()
    {
        base.Start();
        this.challengeList = this.GetComponentInChildren<BaseItemScroll>(true);
        this.scrollbar = this.GetComponentInChildren<BaseScrollbar>(true);
        var texts = this.GetComponentsInChildren<BaseText>(true);
        texts[0].text.SetText(UIStrings.Challenges);
        UIUtils.SetFontSize(texts[0].text, UIUtils.LargeFontSize);
        this.selectionText = texts[1];
        UIUtils.SetFontSize(this.selectionText.text, 0.5f * (UIUtils.MediumFontSize + UIUtils.SmallFontSize));

        // Start identifies footer buttons before challengeList creates its button copies.
        var buttons = this.GetComponentsInChildren<BaseButton>(true);
        var backButton = buttons[buttons.Length - 2];
        this.playButton = buttons[buttons.Length - 1];
        backButton.text.SetText(UIStrings.Back);
        UIUtils.SetFontSize(backButton.text, UIUtils.MediumFontSize);
        backButton.btn.onClick.AddListener(this.GoBack);
        this.playButton.text.SetText("Play");
        UIUtils.SetFontSize(this.playButton.text, UIUtils.MediumFontSize);
        this.playButton.btn.onClick.AddListener(this.PlaySelectedChallenge);

        this.challengeList.ItemFontSize = UIUtils.SmallFontSize;
        this.challengeList.SetupItemList(this.SelectChallenge, VisibleChallenges, this.challenges, this.FormatChallengeLabel);
        foreach (var button in this.challengeList.GetComponentsInChildren<BaseButton>(true))
        {
            button.text.color = Color.black;
        }
        this.scrollbar.scrollbar.onValueChanged.AddListener(this.challengeList.Scroll);
        this.selectedCallbacks.SetCallbacks(new Action[] { this.OnEnterMenu });
        this.deselectedCallbacks.SetCallbacks(new Action[] {
            () => UIManager.instance.TurnMeOff(this)
        });
        this.OnEnterMenu();
    }

    // OnEnterMenu refreshes available challenge names and restores selection without starting a session.
    void OnEnterMenu()
    {
        // OnEnterMenu discovers challenge files through IOHelpers.GetAllChallenges without loading gameplay.
        this.challenges = IOHelpers.GetAllChallenges()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        this.RefreshBestRuns();
        this.challengeList.virtualContainer.UpdateLabels(this.challenges);
        this.scrollbar.scrollbar.SetValueWithoutNotify(0f);
        this.challengeList.Scroll(0f);
        this.scrollbar.scrollbar.interactable = this.challenges.Length > VisibleChallenges;
        if (!this.challenges.Contains(this.selectedChallenge))
        {
            this.selectedChallenge = this.challenges.FirstOrDefault();
        }
        this.UpdateSelection();
        UIManager.instance.SwitchToMe(this);
    }

    // RefreshBestRuns matches available Lua contents to the fastest saved runs without executing challenge logic.
    void RefreshBestRuns()
    {
        this.bestRuns.Clear();
        ChallengeProgressSave save;
        try
        {
            save = new ChallengeProgressStore(Path.Combine(Application.persistentDataPath, "challenge-progress.json")).Load();
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is Newtonsoft.Json.JsonException)
        {
            Debug.LogWarning("Could not load challenge best times: " + error.Message);
            return;
        }
        foreach (string name in this.challenges)
        {
            try
            {
                byte[] contents = File.ReadAllBytes(Path.Combine(IOHelpers.GetChallengesDirectory(), name + UIStrings.ChallengeExtension));
                string hash = ChallengeContentHash.Compute(contents, Array.Empty<byte>());
                var best = save.challenges
                    .Where(progress => progress.challengeHash == hash && progress.challengeLogicVersion == ChallengeLuaMetadata.CurrentLogicVersion)
                    .Select(progress => progress.bestRun)
                    .Where(run => run != null && run.hasTime && !double.IsNaN(run.timeSeconds) &&
                        !double.IsInfinity(run.timeSeconds) && run.timeSeconds >= 0)
                    .OrderBy(run => run.timeSeconds).FirstOrDefault();
                if (best != null) { this.bestRuns[name] = best; }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning("Could not read challenge " + name + ": " + error.Message);
            }
        }
    }

    // FormatChallengeLabel displays the best time and its model name and total chip count beside the challenge name.
    string FormatChallengeLabel(string name)
    {
        if (!this.bestRuns.TryGetValue(name, out var run)) { return name + "  |  Best: --"; }
        string modelName = string.IsNullOrWhiteSpace(run.modelName) ? "Unnamed model" : run.modelName;
        string chips = run.chipCounts == null ? "--" : run.chipCounts.Sum(chip => chip.count).ToString(CultureInfo.InvariantCulture);
        return name + "  |  Best: " + run.timeSeconds.ToString("F2", CultureInfo.InvariantCulture) +
            " s  |  Model: " + modelName + "  |  Chips: " + chips;
    }

    // SelectChallenge selects a listed challenge and refreshes the selection display.
    void SelectChallenge(string name)
    {
        this.selectedChallenge = name;
        this.UpdateSelection();
    }

    // UpdateSelection shows the selected challenge or empty state and enables Play when available.
    void UpdateSelection()
    {
        this.selectionText.text.SetText(this.selectedChallenge == null
            ? "No challenges found. Add challenge files to StreamingAssets/Challenges."
            : "Selected: " + this.selectedChallenge);
        this.playButton.btn.interactable = this.selectedChallenge != null;
    }

    // PlaySelectedChallenge launches the selected challenge through the sandbox startup flow.
    void PlaySelectedChallenge()
    {
        if (this.selectedChallenge == null) { return; }
        SandboxMenu.PlayChallenge(this.selectedChallenge);
    }

    // GoBack returns to the main menu.
    void GoBack() => GoToMainMenu.Function(false);

    // IsActive reports whether the challenge menu is active.
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;
    // OnStartReceiving keeps the initialized challenge controls unchanged when input focus begins.
    void InputReceiver.OnStartReceiving() { }
    // OnStopReceiving keeps challenge selection unchanged when input focus ends.
    void InputReceiver.OnStopReceiving() { }
    // HandleInputs handles challenge launch, back navigation and mouse-wheel scrolling.
    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { this.GoBack(); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            this.PlaySelectedChallenge();
            return;
        }
        if (this.challenges.Length > VisibleChallenges)
        {
            this.scrollbar.scrollbar.value = Mathf.Clamp01(
                this.scrollbar.scrollbar.value - 0.1f * Input.GetAxis("Mouse ScrollWheel"));
        }
    }
}
