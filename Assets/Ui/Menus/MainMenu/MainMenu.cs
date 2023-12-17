using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : BaseMainMenu, InputReceiver
{
    float numberOfButtons = 5f;
    protected override void Setup()
    {
        base.Setup();
        float btnHeight = 1 / (this.numberOfButtons + 1);
        this.vProp = new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            new VirtualProp(PropType.Image, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle)),
                new VirtualProp(PropType.Panel, -1f, Vector2Int.right,
                    new VirtualProp(PropType.Panel, 0.2f),
                    new VirtualProp(PropType.Panel, 0.6f, Vector2Int.down,
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToSingleplayer)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToEditor)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToSettings))
                    //new VirtualProp(PropType.Button, btnHeight, typeof(GoToSingleplayer)),
                    //new VirtualProp(PropType.Button, btnHeight, typeof(GoToSingleplayer))
                    )
                )
            //new VirtualProp(PropType.Button, 0.2f, typeof(GoToMultiplayer)),
            //new VirtualProp(PropType.Button, 0.2f, typeof(GoToEditor)),
            //new VirtualProp(PropType.Button, 0.2f, typeof(GoToOptions)),
            //new VirtualProp(PropType.Button, 0.2f, typeof(Quit)),
            )
        );
    }

    override protected void Start()
    {
        base.Start();

        UIManager uiManager = Camera.main.GetComponent<UIManager>();
        uiManager.SwitchToMe(this);
        Action[] onSelectedCallbacks = new Action[] { () => UIManager.instance.SwitchToMe(this), () => GameManager.Instance.UpdateSettings() };
        this.selectedCallbacks.SetCallbacks(onSelectedCallbacks);
        this.selectedCallbacks.Invoke();
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // TODO: somehow this is being triggered along with the settings menu KeyDown(KeyCode.Escape)
            //print($"MainMenu KEYCODE ESCAPE DOWN: Switching to previous menu");
            SwitchToPreviousMenu();
        }
    }
}
