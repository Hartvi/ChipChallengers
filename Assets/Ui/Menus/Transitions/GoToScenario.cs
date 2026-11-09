
public class GoToScenario : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Scenarios;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        GoToScenario.Function();
    }

    public static void Function()
    {
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(ScenarioMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToScenario));
    }
}

