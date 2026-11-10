using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;


public class EditorMenu : BaseMenu, InputReceiver
{
    public static EditorMenu Instance;

    NetworkManager manager;

    private Camera _camera;
    CoreChip entryCameraCore;
    public CommonChip _selectedChip;
    string pendingChipSelection;
    public CommonChip selectedChip
    {
        get
        {
            return this._selectedChip;
        }
        set
        {
            this._selectedChip = value;
            if (value != null)
            {
                this.lastSelectedPosition = value.transform.position;
                this.FocusSelectedChip();
            }
        }
    }
    Vector3 lastSelectedPosition = Vector3.zero;
    [SerializeField, Min(0.1f)] float focusSharpness = 10f;
    bool focusPending;

    // FocusSelectedChip requests a smooth orbit-camera rotation toward the selected chip.
    void FocusSelectedChip()
    {
        if (this._camera != null && this.selectedChip != null &&
            GameManager.cameraMoveMode == CameraMoveMode.Follow &&
            (this._camera.transform.position - this.lastSelectedPosition).sqrMagnitude > 0.0001f)
        {
            this.focusPending = true;
        }
    }

    // LateUpdate eases the camera toward the focus target with frame-rate-independent quaternion interpolation.
    void LateUpdate()
    {
        if (!this.focusPending) { return; }
        if (this._camera == null || this.selectedChip == null || GameManager.cameraMoveMode != CameraMoveMode.Follow)
        {
            this.focusPending = false;
            return;
        }
        this.lastSelectedPosition = this.selectedChip.transform.position;
        Vector3 direction = this.lastSelectedPosition - this._camera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f) { this.focusPending = false; return; }
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        float blend = 1f - Mathf.Exp(-this.focusSharpness * Time.unscaledDeltaTime);
        this._camera.transform.rotation = Quaternion.Slerp(this._camera.transform.rotation, target, blend);
        if (Quaternion.Angle(this._camera.transform.rotation, target) < 0.1f)
        {
            this._camera.transform.rotation = target;
            this.focusPending = false;
        }
    }

    public HighlighterContainer highlighter;

    public CreateChipPanel CreateChipPanel;
    private LoadPanel LoadPanel;
    private SavePanel SavePanel;

    private VariablePanel VariablePanel;
    private ScriptPanel ScriptPanel;
    private ChipPanel ChipPanel;
    private ControlsPanel ControlsPanel;

    public override void Setup()
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
                            new VirtualProp(PropType.Panel, 1f, typeof(ScriptPanel))
                        )
                    ),
                    new VirtualProp(PropType.Panel, -1f, right, typeof(PanelSwitcher),
                        new VirtualProp(PropType.Button, 1 / 3f),
                        new VirtualProp(PropType.Button, 1 / 3f),
                        new VirtualProp(PropType.Button, 1 / 3f)
                    )
                ),
                new VirtualProp(PropType.Panel, 0.125f, typeof(CreateChipPanel))
            ),
            new VirtualProp(PropType.Panel, 1f, typeof(LoadPanel)),
            new VirtualProp(PropType.Panel, 1f, typeof(SavePanel)),
            new VirtualProp(PropType.Panel, 1f, right, typeof(ControlsPanel))
        );
    }

    protected override void Start()
    {
        base.Start();
        this.manager = GameObject.FindObjectOfType<NetworkManager>();

        // persistent object/variables
        EditorMenu.Instance = this;

        this._camera = Camera.main;

        this.highlighter = this.gameObject.AddComponent<HighlighterContainer>();
        this.highlighter.InstantiateHighlighters();

        this.CreateChipPanel = this.GetComponentInChildren<CreateChipPanel>();

        this.VariablePanel = this.GetComponentInChildren<VariablePanel>(includeInactive: true);
        this.ChipPanel = this.GetComponentInChildren<ChipPanel>(true);
        this.ScriptPanel = this.GetComponentInChildren<ScriptPanel>(true);

        this.SavePanel = this.GetComponentInChildren<SavePanel>();
        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();

        Action[] selectedChipCallbacks = new Action[] {
            this.StartOnEnterMenu,
            () => UIManager.instance.SwitchToMe(this)
        };
        this.selectedCallbacks.SetCallbacks(selectedChipCallbacks);
        this.selectedCallbacks.Invoke();

        Action[] deselectedChipCallbacks = new Action[] { this.OnLeaveMenu };
        this.deselectedCallbacks.SetCallbacks(deselectedChipCallbacks);

        this.ControlsPanel = this.gameObject.GetComponentInChildren<ControlsPanel>(true);
    }

    void StartOnEnterMenu()
    {
        Debug.Assert(this.isActiveAndEnabled);
        StartCoroutine(AsyncEnterMenu());
    }

    private IEnumerator AsyncEnterMenu()
    {
        bool startingHost = !manager.isNetworkActive && GameManager.isHost;
        if (!manager.isNetworkActive)
        {
            if (GameManager.isHost) { manager.StartHost(); }
            else { manager.StartClient(); }
        }
        while (NetworkClient.localPlayer == null)
        {
            yield return null;
        }
        while (CoreChip.ClientCoreChip.IsRebuilding ||
               CoreChip.ClientCoreChip.loadedModelString == null)
        {
            yield return null;
        }
        while (true)
        {
            bool areChipsLoadedAndBuilt = true;
            foreach (var c in CoreChip.ClientCoreChip.AllChips)
            {
                if (c.equivalentVirtualChip == null)
                {
                    areChipsLoadedAndBuilt = false;
                }
            }
            if (!areChipsLoadedAndBuilt)
            {
                yield return null;
            }
            else { break; }
        }
        if (startingHost)
        {
            var core = CoreChip.ClientCoreChip;
            core.CmdResetToDefaultLocation();
            while (core.HasPendingPoseReset || core.IsRebuilding ||
                   core.srvResetCounter != core.cltResetCounter)
            {
                yield return null;
            }
        }
        this.OnEnterMenu();
    }
    void OnEnterMenu()
    {
        CoreChip.ClientCoreChip.CmdFreezeClientModel();
        GameManager.cameraMoveMode = CameraMoveMode.Follow;
        // TODO: options: set framerate, sound level
        Application.targetFrameRate = 24;

        this.highlighter.ParentHighlighter.SetActive(true);

        //print($"Varpanel: {VariablePanel}, ");
        Action[] onLoadedCallbacks = new Action[] {
            // Model-dependent refresh runs in afterBuildListeners instead.
        };
        this.LoadPanel.SetOnLoadedCallbacks(onLoadedCallbacks);

        //print($"Setting editormenu callback to model changed");
        Action[] afterBuildListeners = new Action[] {
            this.RefreshChipSelection,
            this.VariablePanel.RefreshModel,
            this.ScriptPanel.RefreshModel

        };

        // core stuff:
        var core = CoreChip.ClientCoreChip;
        core.freeze = true;
        //core.rb.isKinematic = true;
        // TODO: clean up callbacks, etc after leacing this menu
        core.SetAfterBuildListeners(afterBuildListeners);
        if (this.entryCameraCore != null) { this.entryCameraCore.ResetCompleted -= this.PositionCameraOnEntry; }
        this.entryCameraCore = core;
        core.ResetCompleted += this.PositionCameraOnEntry;
        // TODO: update value, option, etc in the editor to how it should look like.
        // atm it's only angle and colour that is visibly changed after rebuilding

        this.selectedChip = this.highlighter.SelectVChip("a");

        // OnEnterMenu levels both the physics body and Transform after clearing motion before rebuilding.
        core.CmdResetPose(core.rb.position + Vector3.up, Quaternion.identity);

        // slow down physics to save power
        //Time.fixedDeltaTime = 0.1f;
    }

    // PositionCameraOnEntry frames the core once after entering the editor without repositioning later selections or edits.
    void PositionCameraOnEntry()
    {
        if (this.entryCameraCore == null) { return; }
        var core = this.entryCameraCore;
        core.ResetCompleted -= this.PositionCameraOnEntry;
        this.entryCameraCore = null;
        if (!this.isActiveAndEnabled) { return; }
        this.focusPending = false;
        CoreCameraPlacement.Place(this._camera, core.transform, 6f, 3f);
    }

    void OnLeaveMenu()
    {
        this.pendingChipSelection = null;
        if (this.entryCameraCore != null) { this.entryCameraCore.ResetCompleted -= this.PositionCameraOnEntry; }
        this.entryCameraCore = null;
        this.highlighter.ParentHighlighter.SetActive(false);

        // leaving online mode back to unspawned core
        if(NetworkClient.localPlayer == null) { return; }
        var core = CoreChip.ClientCoreChip;
        core.VirtualModel.SaveThisModelToFile(UIStrings.Backup + UIStrings.ModelExtension);
        core.freeze = false;
    }

    // New chips can only be highlighted once their runtime objects have been built.
    public void SelectChipAfterBuild(string chipId)
    {
        this.pendingChipSelection = chipId;
    }

    void RefreshChipSelection()
    {
        string chipId = this.pendingChipSelection ?? this.selectedChip.equivalentVirtualChip.id;
        this.pendingChipSelection = null;
        this.selectedChip = this.highlighter.SelectVChip(chipId);
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
        Camera cam = Camera.main;
        cam.transform.position += 0.55f * Input.mouseScrollDelta.y * cam.transform.forward;

        if (Input.GetMouseButton(1))
        {
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");

            if (GameManager.cameraMoveMode == CameraMoveMode.Free)
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
            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? Time.deltaTime * 9f : Time.deltaTime * 3f;

            cam.transform.position = cam.transform.position + sensitivity * deltaPos;

            if (GameManager.cameraMoveMode == CameraMoveMode.Follow)
            {
                float sensitivity2 = 5f;
                cam.transform.RotateAround(this.lastSelectedPosition, Vector3.up, moveX * sensitivity2);
                cam.transform.RotateAround(this.lastSelectedPosition, cam.transform.right, -moveY * sensitivity2);
                this.FocusSelectedChip();
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
                this.SavePanel.ActivatePanel(GameManager.Instance.GetModel());
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                GoToSandbox.Function();
            }
#else
            if (Input.GetKeyDown(KeyCode.S))
            {

                this.SavePanel.ActivatePanel(GameManager.Instance.GetModel());
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                GoToSandbox.Function();
            }
#endif
            if (Input.GetKeyDown(KeyCode.Z))
            {
#if UNITY_EDITOR
                var ccc = CoreChip.ClientCoreChip;
                ccc.CmdLoadString(ccc.history.Undo());
#else
            try
            {
                var ccc = CoreChip.ClientCoreChip;
                ccc.CmdLoadString(ccc.history.Undo());
            }
            catch {}
#endif
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
#if UNITY_EDITOR
                var ccc = CoreChip.ClientCoreChip;
                ccc.CmdLoadString(ccc.history.Redo());
#else
                try
                {
                    var ccc = CoreChip.ClientCoreChip;
                    ccc.CmdLoadString(ccc.history.Redo());
                }
                catch {}
#endif
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                VChip vc = this.selectedVChip;
                var core = CoreChip.ClientCoreChip;
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

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            VChip vc = this.selectedVChip;
            var core = CoreChip.ClientCoreChip;
            VModel vm = core.VirtualModel;

            if (!string.IsNullOrWhiteSpace(vc.parentId))
            {
                this.selectedChip = this.highlighter.SelectVChip(vc.parentId);
                vm.DeleteChip(vc.id);
            }
        }

    }

    void InputReceiver.OnStartReceiving() { }
    void InputReceiver.OnStopReceiving() { }
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
            this.GetComponentInChildren<ControlsPanel>(true).gameObject.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            GameManager.cameraMoveMode = CameraMoveMode.Follow;
            this.FocusSelectedChip();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            GameManager.cameraMoveMode = CameraMoveMode.Free;
        }

        if (Input.GetMouseButtonDown(0) && !TopUI.IsOnUI)  // 0 means left mouse button
        {
            // if not adding new chip, then try highlighting chip
            if (!this.DisplayChipMenu())
            {
                CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

                if (this.highlighter.HighlightChip(clickedObject, out CommonChip sc))
                {
                    this.selectedChip = sc;
                }
                else
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

