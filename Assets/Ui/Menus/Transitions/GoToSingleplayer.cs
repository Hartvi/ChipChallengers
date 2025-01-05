
public class GoToSingleplayer : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Singleplayer;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        GoToSingleplayer.Function();
    }

    public static void Function()
    {
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSingleplayer));
    }
}

