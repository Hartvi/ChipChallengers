using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class ConnectToHost : BaseTransition
{
    Coroutine connectionRoutine;
    public bool connecting { get; private set; }

    public override void Setup()
    {
        base.Setup();
        this.text.text = UIStrings.ConnectToServer;
        UIUtils.SetFontSize(this.text, UIUtils.MediumFontSize);
    }

    protected override void Execute()
    {
        this.Function();
    }

    public void Function()
    {
        if (this.connecting)
        {
            this.CancelConnection();
            return;
        }

        GameManager.PlayContext.SelectSandbox();
        GameManager.isHost = false;
        NetworkManager.singleton.StartClient();
        this.text.text = "Cancel";
        this.connecting = true;
        this.connectionRoutine = StartCoroutine(ConnectToServer());
    }

    public void CancelConnection()
    {
        if (!this.connecting) { return; }

        if (this.connectionRoutine != null)
        {
            StopCoroutine(this.connectionRoutine);
            this.connectionRoutine = null;
        }
        this.connecting = false;
        this.text.text = UIStrings.ConnectToServer;
        NetworkManager.singleton.StopClient();
    }

    IEnumerator ConnectToServer()
    {
        while (true)
        {
            if (NetworkClient.localPlayer == null)
            {
                if (!NetworkClient.isConnecting && !NetworkClient.isConnected)
                {
                    this.connectionRoutine = null;
                    this.connecting = false;
                    this.text.text = UIStrings.ConnectToServer;

                    NetworkManager.singleton.StopClient();
                    yield break;
                }
                yield return null;
            }
            else { break; }
        }
        this.connectionRoutine = null;
        this.connecting = false;
        this.text.text = UIStrings.ConnectToServer;
        BaseMenu.SwitchToMenu(typeof(SandboxMenu));

        BaseTransition.InvokeAfterClickedCallbacks(typeof(ConnectToHost));
    }
}

