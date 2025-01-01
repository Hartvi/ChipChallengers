
public class GoToMultiplayer : BaseTransition
{
    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Multiplayer;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        GoToMultiplayer.Function();
    }

    public static void Function()
    {
        GameManager.isHost = false;
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToMultiplayer));
    }
}
