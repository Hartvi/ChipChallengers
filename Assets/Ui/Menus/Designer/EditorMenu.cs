using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorMenu : BaseMenu
{
    public static EditorMenu Instance;

    private Camera _camera;
    public CommonChip selectedChip;

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

        this.OnEnterMenu();

        Action[] selectedChipCallbacks = new Action[] { CommonChip.FreezeModel, this.OnEnterMenu };
        this.selectedCallbacks.SetCallbacks(selectedChipCallbacks);

        Action[] deselectedChipCallbacks = new Action[] { this.OnLeaveMenu };
        this.deselectedCallbacks.SetCallbacks(deselectedChipCallbacks);
    }

    void OnEnterMenu()
    {
        this.highlighter.ParentHighlighter.SetActive(true);

        print($"Varpanel: {VariablePanel}, ");
        this.LoadPanel.SetOnLoadedCallbacks(new Action[] { this.VariablePanel.ReloadVariables, this.VariablePanel.AddListenersToModel });

        //print($"Setting editormenu callback to model changed");
        Action[] afterBuildListeners = new Action[] {
            () => {
                //print($"selected chip: {this.selectedChip}");
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

    void InEditorInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoToMainMenu.Function();
        }
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

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
        }

        // outside ctrl + something

        if(Input.GetKeyDown(KeyCode.Delete))
        {
            VChip vc = this.selectedVChip;
            CommonChip core = CommonChip.ClientCore;
            VModel vm = core.VirtualModel;

            if (vc.parentId is not null)
            {
                this.selectedChip = this.highlighter.SelectVChip(vc.parentId);
                vm.DeleteChip(vc.id);
            }
        }

    }

    void Update()
    {
        if (!this.ScriptPanel.IsSelected)
        {
            this.InEditorInputs();
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
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            // 
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");
            Camera cam = Camera.main;

            cam.transform.Rotate(2f*moveX*Vector3.up, Space.World);
            cam.transform.Rotate(-2f*moveY*Vector3.right, Space.Self);
            
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

            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? 0.09f : 0.03f;
            cam.transform.position = cam.transform.position + sensitivity * deltaPos;
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

