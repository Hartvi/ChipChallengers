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
        EditorMenu.Instance = this;
        this.CreateChipPanel = this.GetComponentInChildren<CreateChipPanel>();

        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();
        this.SavePanel = this.GetComponentInChildren<SavePanel>();

        Action[] selectedChipCallbacks = new Action[] { CommonChip.FreezeModel };
        this.SetThisMenuSelectedCallbacks(selectedChipCallbacks);

        base.Start();

        this.highlighter = this.gameObject.AddComponent<HighlighterContainer>();
        this.highlighter.InstantiateHighlighters();

        this._camera = Camera.main;

        print($"Setting editormenu callback to model changed");
        Action[] afterBuildListeners = new Action[] { 
            () => {
                this.selectedChip = this.highlighter.SelectVChip(this.selectedChip.equivalentVirtualChip.id);
            },
        };

        CommonChip.ClientCore.SetAfterBuildListeners(afterBuildListeners);
        // TODO: update value, option, etc in the editor to how it should look like.
        // atm it's only angle and colour that is visibly changed after rebuilding

        // set core to correct orientation
        CommonChip.ClientCore.transform.rotation = Quaternion.identity;

        this.selectedChip = this.highlighter.SelectVChip("a");

        // rebuild the model so it's not flat when entering designer mode
        CommonChip.ClientCore.TriggerSpawn(CommonChip.ClientCore.VirtualModel, true);

        this.VariablePanel = this.GetComponentInChildren<VariablePanel>();
        this.ChipPanel = this.GetComponentInChildren<ChipPanel>();
        this.ScriptPanel = this.GetComponentInChildren<ScriptPanel>();

        this.LoadPanel.SetOnLoadedCallbacks(new Action[] { this.VariablePanel.ReloadVariables, this.VariablePanel.AddListenersToModel });
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

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject.GetComponent<T>();
        }
        
        return default(T);
    }

    void Update()
    {
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

        }

        if (Input.GetMouseButtonDown(0) && !TopUI.IsOnUI)  // 0 means left mouse button
        {
            // if not adding new chip, then try highlighting chip
            if (!this.DisplayChipMenu())
            {
                CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();
                this.selectedChip = this.highlighter.HighlightChip(clickedObject);
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

