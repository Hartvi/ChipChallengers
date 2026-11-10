using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assimp;

public class ChallengePanel : BaseScrollMenu
{

    CallbackArray OnLoadedCallbacks = new CallbackArray(false);

    protected override void Start()
    {
        base.Start();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllChallenges());

        this.btns[0].btn.onClick.AddListener(this.LoadChallenge);

        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;

        this.input.placeholder.SetText(UIStrings.EnterChallengeName);
    }

    void LoadChallenge()
    {
        SandboxMenu.myVChallenge.LoadNewChallenge(this.input.input.text + UIStrings.ChallengeExtension);

        this.DeactivatePanel();

        this.OnLoadedCallbacks.Invoke();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (this.itemScroll != null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllChallenges());
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
