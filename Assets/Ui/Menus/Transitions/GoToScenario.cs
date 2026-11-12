
public class GoToChallenges : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Challenges;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        GoToChallenges.Function();
    }

    public static void Function()
    {
        if (DrivingIntroduction.ShouldOffer)
        {
            BaseMenu.SwitchToMenu(typeof(IntroductionOfferMenu));
            return;
        }
        Browse();
    }

    public static void Browse()
    {
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(ChallengeMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToChallenges));
    }
}

