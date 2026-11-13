using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MainMenu : BaseMainMenu, InputReceiver
{
    public static bool goToMultiplayerMenu = false;

    float numberOfButtons = 7f;
    [SerializeField] Vector3 menuCameraPosition = new Vector3(35f, 32f, -65f);
    [SerializeField] Vector3 menuCameraTarget = new Vector3(-30f, 1f, 25f);
    Material buttonTextMaterial;

    public override void Setup()
    {
        base.Setup();
        float btnHeight = 1 / (this.numberOfButtons + 1);
        this.vProp = new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
            new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle)),
                new VirtualProp(PropType.Panel, -1f, Vector2Int.right,
                    new VirtualProp(PropType.Panel, 0.2f),
                    new VirtualProp(PropType.Panel, 0.6f, Vector2Int.down,
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToIntroduction)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToChallenges)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToSandbox)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToMultiplayer)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToEditor)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(GoToSettings)),
                        new VirtualProp(PropType.Button, btnHeight, typeof(QuitGame))
                    )
                )
            )
        );
    }

    override protected void Start()
    {
        base.Start();
        var buttonSprite = Resources.Load<Sprite>("UI/Art/tall_field_transparent");
        foreach (var button in this.GetComponentsInChildren<BaseButton>(true))
        {
            button.image.sprite = buttonSprite;
            if (this.buttonTextMaterial == null)
            {
                this.buttonTextMaterial = new Material(button.text.fontSharedMaterial) {
                    name = "Main menu text shadow", hideFlags = HideFlags.HideAndDontSave
                };
                this.buttonTextMaterial.EnableKeyword("UNDERLAY_ON");
                this.buttonTextMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.75f));
                this.buttonTextMaterial.SetFloat("_UnderlayOffsetX", 0.7f);
                this.buttonTextMaterial.SetFloat("_UnderlayOffsetY", -0.7f);
                this.buttonTextMaterial.SetFloat("_UnderlaySoftness", 0.3f);
            }
            button.text.fontSharedMaterial = this.buttonTextMaterial;
            button.text.UpdateMeshPadding();
        }
        print("ENTER MAIN MENU");

        UIManager uiManager = Camera.main.GetComponent<UIManager>();
        uiManager.SwitchToMe(this);
        Action[] onSelectedCallbacks = new Action[] {
            () => UIManager.instance.SwitchToMe(this),
            () => GameManager.Instance.UpdateSettings(),
            this.PositionMenuCamera
        };
        this.selectedCallbacks.SetCallbacks(onSelectedCallbacks);

        // load default map so it show in sandbox, settings, editor and all the other menus!!!
        SandboxMenu.myVMap.LoadNewMap(VMap.DefaultFileName);
        this.selectedCallbacks.Invoke();
        if (global::MainMenu.goToMultiplayerMenu)
        {
            global::MainMenu.goToMultiplayerMenu = false;
            GoToMultiplayer.Function();
        }
    }

    // Restore a level, elevated view whenever the main menu is selected.
    void PositionMenuCamera()
    {
        var camera = Camera.main;
        if (camera == null) { return; }
        camera.transform.position = this.menuCameraPosition;
        camera.transform.LookAt(this.menuCameraTarget, Vector3.up);
        var blur = camera.GetComponent<MenuBackgroundBlur>();
        if (blur == null) { blur = camera.gameObject.AddComponent<MenuBackgroundBlur>(); }
        blur.Menu = this;
    }

    void OnDestroy()
    {
        if (this.buttonTextMaterial != null) { Destroy(this.buttonTextMaterial); }
    }

    void InputReceiver.OnStartReceiving() { }
    void InputReceiver.OnStopReceiving() { }
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // TODO: somehow this is being triggered along with the settings menu KeyDown(KeyCode.Escape)
            SwitchToPreviousMenu();
        }
    }
}
