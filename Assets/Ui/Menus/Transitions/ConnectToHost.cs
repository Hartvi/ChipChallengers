using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class ConnectToHost : BaseTransition
{
    bool stopConnection = false;
    public bool connecting = false;

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.ConnectToServer;
        this.text.fontSize = UIUtils.MediumFontSize;
    }

    protected override void Execute()
    {
        this.Function();
    }

    public void Function()
    {
        GameManager.isHost = false;
        NetworkManager.singleton.StartClient();
        this.text.text = "Connecting...";
        this.connecting = true;
        StartCoroutine(ConnectToServer());
    }

    IEnumerator ConnectToServer()
    {
        while (true)
        {
            if (NetworkClient.localPlayer == null)
            {
                this.stopConnection = !NetworkClient.isConnecting;
                if (this.stopConnection && !NetworkClient.isConnected)
                {
                    this.connecting = false;
                    this.stopConnection = false;
                    this.text.text = UIStrings.ConnectToServer;

                    NetworkManager.singleton.StopClient();
                    print($"StopClient");
                    yield break;
                    //BaseMenu.SwitchToMenu(typeof(MainMenu));
                }
                yield return null;
            }
            else { break; }
        }
        print($"Switching to singleplayer");
        BaseMenu.SwitchToMenu(typeof(SingleplayerMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(ConnectToHost));
    }
}

