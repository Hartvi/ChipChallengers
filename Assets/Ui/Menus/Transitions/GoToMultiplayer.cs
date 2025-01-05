using Mirror;

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
        global::MainMenu.goToMultiplayerMenu = true;
        BaseMenu.SwitchToMenu(typeof(MultiplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(GoToMultiplayer));
        GameManager.isHost = false;
        if (NetworkClient.isConnected)
        {
            if (NetworkServer.activeHost)
            {
                print($"ConnectToHost: Stopping host");
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active)
            {
                print($"ConnectToHost: Stopping client");
                NetworkManager.singleton.StopClient();
            }
        }
    }
}
