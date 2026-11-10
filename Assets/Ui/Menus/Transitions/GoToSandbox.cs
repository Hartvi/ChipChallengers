
public class GoToSandbox : BaseTransition
{

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.Sandbox;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        GoToSandbox.Function(regularSandbox: true);
    }

    // Function resumes the current play context unless an explicit sandbox selection clears it.
    public static void Function(bool regularSandbox = false)
    {
        if (regularSandbox) { GameManager.PlayContext.SelectSandbox(); }
        GameManager.isHost = true;
        // inherit is host from other menus
        BaseMenu.SwitchToMenu(typeof(SandboxMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToSandbox));
    }
}
