public class GoToIntroduction : BaseTransition
{
    public override void Setup()
    {
        base.Setup();
        this.text.SetText("Learn to play");
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute() => DrivingIntroduction.Launch();
}
