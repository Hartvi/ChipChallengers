using System;
using UnityEngine;

public class IntroductionOfferMenu : BaseMenu, InputReceiver
{
    IntroductionOverlay overlay;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f);
    }

    protected override void Start()
    {
        base.Start();
        this.overlay = IntroductionOverlay.Create(this.transform);
        this.selectedCallbacks.SetCallbacks(new Action[] { this.ShowOffer });
        this.deselectedCallbacks.SetCallbacks(new Action[] { () => UIManager.instance.TurnMeOff(this) });
        this.ShowOffer();
    }

    void ShowOffer()
    {
        UIManager.instance.SwitchToMe(this);
        this.overlay.ShowModal("First time driving?",
            "Learn the controls on a guided lap of the Industrial Test Track.\n\n" +
            "You can replay it anytime from Learn to play.",
            "Start introduction", DrivingIntroduction.Launch, "Browse challenges", this.Browse,
            "Back", () => GoToMainMenu.Function(false));
    }

    void Browse()
    {
        DrivingIntroduction.DismissOffer();
        GoToChallenges.Browse();
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;
    void InputReceiver.OnStartReceiving() { }
    void InputReceiver.OnStopReceiving() { }
    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { GoToMainMenu.Function(false); }
    }
}
