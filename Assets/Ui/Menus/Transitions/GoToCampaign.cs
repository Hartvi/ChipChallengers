
public class GoToCampaign : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Campaign;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        GoToCampaign.Function();
    }

    public static void Function()
    {
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(CampaignMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToCampaign));
    }
}

