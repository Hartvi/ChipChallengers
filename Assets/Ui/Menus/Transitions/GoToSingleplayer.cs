
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
        GoToSingleplayer.Function(regularSingleplayer: true);
    }

    // Function resumes the current play context unless an explicit singleplayer selection clears it.
    public static void Function(bool regularSingleplayer = false)
    {
        if (regularSingleplayer) { GameManager.PlayContext.SelectSingleplayer(); }
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSingleplayer));
    }
}

