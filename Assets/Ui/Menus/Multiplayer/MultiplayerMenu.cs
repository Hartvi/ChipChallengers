using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class MultiplayerMenu : BaseMenu, InputReceiver
{
    ConnectToHost cth;



    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            new VirtualProp(PropType.Image, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.3f),
                new VirtualProp(PropType.Panel, -1f, Vector2Int.right,
                    new VirtualProp(PropType.Panel, 0.2f),
                    new VirtualProp(PropType.Panel, 0.6f, Vector2Int.down,
                        new VirtualProp(PropType.Text, 0.2f, typeof(ConnectToServer)),
                        new VirtualProp(PropType.Input, 0.2f, typeof(IPAddress)),
                        new VirtualProp(PropType.Button, 0.2f, typeof(ConnectToHost)),
                        new VirtualProp(PropType.Button, 0.2f)
                    )
                )
            )
        );
    }

    protected override void Start()
    {
        this.cth = this.GetComponentInChildren<ConnectToHost>();
        var buttons = this.GetComponentsInChildren<BaseButton>();
        var backButton = buttons[buttons.Length - 1];
        backButton.text.SetText(UIStrings.Back);
        backButton.btn.onClick.AddListener(this.GoBack);

        Action[] selectedChipCallbacks = new Action[] {
            () => UIManager.instance.SwitchToMe(this)
        };
        this.selectedCallbacks.SetCallbacks(selectedChipCallbacks);
        foreach (var t in this.GetComponentsInChildren<TMP_Text>())
        {
            UIUtils.SetFontSize(t, UIUtils.MediumFontSize);
        }
        UIManager.instance.SwitchToMe(this);
    }

    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (cth.connecting)
            {
                this.cth.CancelConnection();
                return;
            }
            this.GoBack();
        }
    }

    void GoBack()
    {
        this.cth.CancelConnection();
        GoToMainMenu.Function(false);
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.OnStartReceiving()
    {
    }

    void InputReceiver.OnStopReceiving()
    {
    }
}
