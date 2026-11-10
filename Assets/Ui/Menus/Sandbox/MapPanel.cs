using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assimp;

public class MapPanel : BaseScrollMenu
{

    CallbackArray OnLoadedCallbacks = new CallbackArray(false);
    BaseText challengeWarning;

    // Setup adds a warning above the map list and leaves room for the challenge-exit button label.
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
        this.challengeWarning = this.GetComponentInChildren<BaseText>(true);
        UIUtils.SetFontSize(this.challengeWarning.text, UIUtils.SmallFontSize);
        this.UpdateChallengeWarning();

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
        bool wasChallenge = GameManager.PlayContext.IsChallenge;
        if (!SandboxMenu.myVMap.LoadNewMap(this.input.input.text + UIStrings.MapExtension)) { return; }

        this.DeactivatePanel();

        this.OnLoadedCallbacks.Invoke();
        if (wasChallenge && !GameManager.PlayContext.IsChallenge)
        {
            DisplaySingleton.Instance.DisplayText(text => {
                DisplaySingleton.BasicSmallModification(text);
                DisplaySingleton.NoOverflowEtc(text);
                var parent = (RectTransform)text.rectTransform.parent;
                text.rectTransform.sizeDelta = new Vector2(parent.rect.width * 0.9f, parent.rect.height * 0.1f);
                text.SetText("Challenge ended — regular sandbox");
            }, 3f, "challenge-map-exit");
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        this.UpdateChallengeWarning();
        if (this.itemScroll?.virtualContainer != null)
        {
            this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllMaps());
            this.itemScroll.Scroll(this.scrollbar.scrollbar.value);
        }
    }

    // UpdateChallengeWarning explains the mode change before a map is loaded from an active challenge.
    void UpdateChallengeWarning()
    {
        if (this.challengeWarning == null || this.btns == null) { return; }
        bool inChallenge = GameManager.PlayContext.IsChallenge;
        this.btns[0].text.SetText(inChallenge ? "Load map & leave challenge" : "Load map");
        this.challengeWarning.text.SetText(inChallenge
            ? "Loading a map ends the current challenge and returns to regular sandbox."
            : "");
        this.challengeWarning.gameObject.SetActive(inChallenge);

        // Keep the footer fixed while giving the unused warning space back to the list.
        var listRow = this.itemScroll.rParent;
        float panelHeight = listRow.rParent.RT.sizeDelta.y;
        float headerFraction = inChallenge ? 0.1f : 0f;
        float listFraction = 0.8f - headerFraction;
        var size = listRow.RT.sizeDelta;
        size.y = panelHeight * listFraction;
        listRow.RT.sizeDelta = size;
        var position = listRow.RT.anchoredPosition;
        position.y = panelHeight * (0.5f - headerFraction - 0.5f * listFraction);
        listRow.RT.anchoredPosition = position;
        foreach (var child in listRow.rChildren)
        {
            // Scrollbars are rotated: resize in the row's axes, then convert back.
            var childSize = TopProp.Rotate2D(child.RT.sizeDelta, child.vProp.Rotation);
            childSize.y = size.y;
            child.RT.sizeDelta = TopProp.Rotate2D(childSize, child.vProp.Rotation);
        }
        this.itemScroll.RefreshLayout();
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
