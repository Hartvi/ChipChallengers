using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class EditorMenu : BaseMenu, InputReceiver
{
    private CommonChip _core;
    private CommonChip core
    {
        get
        {
            if (this._core is null)
            {
                this._core = CommonChip.ClientCore;
            }
            return this._core;
        }
    }

    private CameraMoveMode cameraMoveMode = CameraMoveMode.Follow;
    public static EditorMenu Instance;

    private Camera _camera;
    public CommonChip _selectedChip;
    public CommonChip selectedChip
    {
        get
        {
            return this._selectedChip;
        }
        set
        {
            this._selectedChip = value;
            if (value is not null)
            {
                this.lastSelectedPosition = value.transform.position;
                //this._camera.transform.LookAt(value.transform.position);
            }
        }
    }
    Vector3 lastSelectedPosition = Vector3.zero;

    public HighlighterContainer highlighter;

    public CreateChipPanel CreateChipPanel;
    private LoadPanel LoadPanel;
    private SavePanel SavePanel;

    private VariablePanel VariablePanel;
    private ScriptPanel ScriptPanel;
    private ChipPanel ChipPanel;

    protected override void Setup()
    {
        base.Setup();
        //this.vProp = new VirtualProp(PropType.Panel, 1f, left,
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            new VirtualProp(PropType.Panel, 1f, left,
                new VirtualProp(PropType.Panel, 0.25f, up,
                    new VirtualProp(PropType.Panel, 0.9f, down,
                        new VirtualProp(PropType.Panel, -1f, zero,
                            new VirtualProp(PropType.Panel, 1f, typeof(ChipPanel)),
                            new VirtualProp(PropType.Panel, 1f, typeof(VariablePanel)),
                            new VirtualProp(PropType.Panel, 1f, typeof(ControlsPanel)),
                            new VirtualProp(PropType.Panel, 1f, typeof(ScriptPanel))
                        )
                    ),
                    new VirtualProp(PropType.Panel, -1f, right, typeof(PanelSwitcher),
                        new VirtualProp(PropType.Button, 1/4f),
                        new VirtualProp(PropType.Button, 1/4f),
                        new VirtualProp(PropType.Button, 1/4f),
                        new VirtualProp(PropType.Button, -1f)
                    )
                ),
                new VirtualProp(PropType.Panel, 0.125f, typeof(CreateChipPanel))
            ),
            new VirtualProp(PropType.Panel, 1f, typeof(LoadPanel)),
            new VirtualProp(PropType.Panel, 1f, typeof(SavePanel))
        );
    }

    protected override void Start()
    {
        base.Start();

        // persistent object/variables
        EditorMenu.Instance = this;

        this._camera = Camera.main;

        this.highlighter = this.gameObject.AddComponent<HighlighterContainer>();
        this.highlighter.InstantiateHighlighters();

        this.CreateChipPanel = this.GetComponentInChildren<CreateChipPanel>();

        this.VariablePanel = this.GetComponentInChildren<VariablePanel>(includeInactive: true);
        this.ChipPanel = this.GetComponentInChildren<ChipPanel>();
        this.ScriptPanel = this.GetComponentInChildren<ScriptPanel>();

        this.SavePanel = this.GetComponentInChildren<SavePanel>();
        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();

        //this.OnEnterMenu();

        Action[] selectedChipCallbacks = new Action[] { CommonChip.FreezeModel, this.OnEnterMenu, () => UIManager.instance.SwitchToMe(this) };
        this.selectedCallbacks.SetCallbacks(selectedChipCallbacks);
        this.selectedCallbacks.Invoke();

        Action[] deselectedChipCallbacks = new Action[] { this.OnLeaveMenu };
        //Action[] deselectedChipCallbacks = new Action[] { this.OnLeaveMenu, () => UIManager.instance.TurnMeOff(this) };
        this.deselectedCallbacks.SetCallbacks(deselectedChipCallbacks);
    }

    void OnEnterMenu()
    {
        // TODO: options: set framerate, sound level
        Application.targetFrameRate = 30;
        
        this.highlighter.ParentHighlighter.SetActive(true);

        print($"Varpanel: {VariablePanel}, ");
        this.LoadPanel.SetOnLoadedCallbacks(new Action[] { this.VariablePanel.ReloadVariables, this.VariablePanel.AddListenersToModel });

        //print($"Setting editormenu callback to model changed");
        Action[] afterBuildListeners = new Action[] {
            () => {
                this.selectedChip = this.highlighter.SelectVChip(this.selectedChip.equivalentVirtualChip.id);
            },
            () => {
                try
                {
                    VariablePanel.ReloadVariables();
                }
                catch
                {
                    Debug.LogWarning($"THIS warning should only appear at startup");
                }
            }

        };

        // core stuff:
        CommonChip core = CommonChip.ClientCore;
        // TODO: clean up callbacks, etc after leacing this menu
        core.SetAfterBuildListeners(afterBuildListeners);
        // TODO: update value, option, etc in the editor to how it should look like.
        // atm it's only angle and colour that is visibly changed after rebuilding

        // set core to correct orientation
        core.transform.rotation = Quaternion.identity;
        core.transform.position += Vector3.up;

        this.selectedChip = this.highlighter.SelectVChip("a");

        // rebuild the model so it's not flat when entering designer mode
        core.TriggerSpawn(core.VirtualModel, true);

        // slow down physics to save power
        Time.fixedDeltaTime = 0.1f;
        this._camera.transform.LookAt(core.transform.position);
    }

    void OnLeaveMenu()
    {
        this.highlighter.ParentHighlighter.SetActive(false);
    }

    public VChip selectedVChip
    {
        get
        {
            if (this.selectedChip is not null)
            {
                return this.selectedChip.equivalentVirtualChip;
            }
            else
            {
                return null;
            }
        }
    }


    public T GetObjectFromScreenClick<T>()
    {
        Ray ray = this._camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = (1 << 5) | (1 << 6) | (1 << 7);

        if (Physics.Raycast(ray, out hit, 1e9f, layerMask))
        {
            return hit.collider.gameObject.GetComponent<T>();
        }
        
        return default(T);
    }

    void SwitchCameraModes()
    {
        if (Input.GetMouseButton(1))
        {
            Camera cam = Camera.main;
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");

            if (this.cameraMoveMode == CameraMoveMode.Free)
            {

                cam.transform.Rotate(2f * moveX * Vector3.up, Space.World);
                cam.transform.Rotate(-2f * moveY * Vector3.right, Space.Self);

            }
            Vector3 deltaPos = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                deltaPos = cam.transform.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                deltaPos = -cam.transform.right;
            }
            if (Input.GetKey(KeyCode.S))
            {
                deltaPos = -cam.transform.forward;
            }
            if (Input.GetKey(KeyCode.D))
            {
                deltaPos = cam.transform.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                deltaPos = -cam.transform.up;
            }
            if (Input.GetKey(KeyCode.E))
            {
                deltaPos = cam.transform.up;
            }
            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? Time.deltaTime*9f : Time.deltaTime*3f;

            cam.transform.position = cam.transform.position + sensitivity * deltaPos;

            if (this.cameraMoveMode == CameraMoveMode.Follow)
            {
                float sensitivity2 = 5f;
                cam.transform.RotateAround(this.lastSelectedPosition, Vector3.up, moveX * sensitivity2);
                cam.transform.RotateAround(this.lastSelectedPosition, cam.transform.right, -moveY * sensitivity2);
                cam.transform.LookAt(this.lastSelectedPosition);
            }
        }

    }

    void InEditorInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoToMainMenu.Function(true);
        }
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (ctrlPressed)
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.E))
            {
                this.SavePanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                this.LoadPanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                GoToSingleplayer.Function();
            }
#else
            if (Input.GetKeyDown(KeyCode.S))
            {

                this.SavePanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.LoadPanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                GoToSingleplayer.Function();
            }
#endif
            if (Input.GetKeyDown(KeyCode.Z))
            {
#if UNITY_EDITOR
                this.LoadPanel.LoadString(HistoryStack.Undo());
#else
            try
            {
                this.LoadPanel.LoadString(HistoryStack.Undo());
            }
            catch {}
#endif
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
#if UNITY_EDITOR
                this.LoadPanel.LoadString(HistoryStack.Redo());
#else
            try
            {
                this.LoadPanel.LoadString(HistoryStack.Redo());
            }
            catch {}
#endif
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                VChip vc = this.selectedVChip;
                CommonChip core = CommonChip.ClientCore;
                VModel vm = core.VirtualModel;

                if (vc.parentId is not null)
                {
                    Clipboard.Copy(vc.id);
                }
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (this.selectedVChip != null)
                {
                    CreateChipPanel.PasteCallback(this.selectedVChip, CreateChipPanel.localDir, shiftPressed);
                }
            }
        }

        // outside ctrl + something

        if(Input.GetKeyDown(KeyCode.Delete))
        {
            VChip vc = this.selectedVChip;
            CommonChip core = CommonChip.ClientCore;
            VModel vm = core.VirtualModel;

            if (!string.IsNullOrWhiteSpace(vc.parentId))
            {
                this.selectedChip = this.highlighter.SelectVChip(vc.parentId);
                vm.DeleteChip(vc.id);
            }
        }

    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        //base.HandleInputs();
        if (!this.ScriptPanel.IsSelected)
        {
            this.InEditorInputs();
            this.SwitchCameraModes();
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            this.cameraMoveMode = CameraMoveMode.Follow;
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            this.cameraMoveMode = CameraMoveMode.Free;
        }

        if (Input.GetMouseButtonDown(0) && !TopUI.IsOnUI)  // 0 means left mouse button
        {
            // if not adding new chip, then try highlighting chip
            if (!this.DisplayChipMenu())
            {
                CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

                if(this.highlighter.HighlightChip(clickedObject, out CommonChip sc))
                {
                    this.selectedChip = sc;
                } else
                {
                    //Debug.Log($"Setting selected chip to NULL!!!");
                    //this.selectedChip = null;
                }
            }
        }

    }

    bool DisplayChipMenu()
    {
        ColourHighlighter clickedObject = this.GetObjectFromScreenClick<ColourHighlighter>();

        if (clickedObject is not null)
        {
            this.CreateChipPanel.Display(Input.mousePosition, clickedObject.localDirection, this.selectedVChip);
            //print($"clicked direction: {clickedObject.localDirection}");
            return true;   
        }
        this.CreateChipPanel.gameObject.SetActive(false);
        return false;
    }

}

