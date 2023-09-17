using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorMenu : BaseMenu
{
    public static EditorMenu Instance;

    private Camera _camera;
    private CommonChip selectedChip;

    private GameObject HighlightingChip, NorthChip, SouthChip, EastChip, WestChip;
    private Action<VChip>[] HighlightCallbacks = new Action<VChip>[] { };

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

        this.AddSelectionCallback(CommonChip.FreezeModel);

        foreach(var c in this.selectedCallbacks)
        {
            c();
        }
        base.Start();

        this._camera = Camera.main;

        this.HighlightingChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = this.HighlightingChip.GetComponent<MeshRenderer>();
        Material m = renderer.material;
        m.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        m.SetTransparent();

        this.HighlightingChip.SetActive(false);

        GameObject.Destroy(this.HighlightingChip.GetComponent<Collider>());

        this.NorthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ColourHighlighter ch = this.NorthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.North;

        renderer = this.NorthChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.0f, 0.0f, 0.8f, 0.9f);
        m.SetTransparent();

        this.NorthChip.SetActive(false);

        this.SouthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.SouthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.South;

        renderer = this.SouthChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.8f, 0.0f, 0.0f, 0.9f);
        m.SetTransparent();

        this.SouthChip.SetActive(false);

        this.EastChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.EastChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.East;

        renderer = this.EastChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.0f, 0.8f, 0.0f, 0.9f);
        m.SetTransparent();

        this.EastChip.SetActive(false);

        this.WestChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.WestChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.West;

        renderer = this.WestChip.GetComponent<MeshRenderer>();
        m = renderer.material;
        m.color = new Color(0.8f, 0.8f, 0.0f, 0.9f);
        m.SetTransparent();

        this.WestChip.SetActive(false);

        print($"Setting editormenu callback to model changed");
        Action[] afterBuildListeners = new Action[] { 
            () => this.SelectVChip(this.selectedChip.equivalentVirtualChip.id),
        };
        CommonChip.ClientCore.SetAfterBuildListeners(afterBuildListeners);
        // TODO: update colour, spring, angle, value, etc in the editor to how it should look like.
        // atm it's only angle that is visibly changed after rebuilding

        // set core to correct orientation
        CommonChip.ClientCore.transform.rotation = Quaternion.identity;
        this.SelectVChip("a");

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


    public void AddHighlightCallback(Action<VChip> vc) {
        this.HighlightCallbacks = this.HighlightCallbacks.Concat(new Action<VChip>[] { vc }).ToArray();
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
            if (Application.isEditor && Input.GetKeyDown(KeyCode.E))
            {
                this.SavePanel.gameObject.SetActive(true);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {

                this.SavePanel.gameObject.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                this.LoadPanel.gameObject.SetActive(true);
            }
        }

        if (Input.GetMouseButtonDown(0) && !TopUI.IsOnUI)  // 0 means left mouse button
        {
            // if not adding new chip, then try highlighting chip
            if (!this.DisplayChipMenu())
            {
                this.HighlightChip();
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

            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? 0.03f : 0.01f;
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

    public void SelectVChip(string chipId)
    {
        CommonChip cc = CommonChip.ClientCore.AllChips.FirstOrDefault(x => x.equivalentVirtualChip.id == chipId) as CommonChip;

        if(cc is null)
        {
            cc = CommonChip.ClientCore;
            //throw new NullReferenceException($"Chip with id {chipId} does not exist.");
        }
        //print($"Selected chip type: {cc.equivalentVirtualChip.ChipType}.");
        //print(cc.transform.rotation.eulerAngles);
        this.selectedChip = cc;

        Vector3 scalingVector = Vector3.one + Vector3.up * 0.1f;

        var hc = this.HighlightingChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale.Multiply(scalingVector);

        hc = this.NorthChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position + GeometricChip.ChipSide*this.selectedChip.transform.forward;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale.Multiply(scalingVector);

        hc = this.SouthChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position - GeometricChip.ChipSide*this.selectedChip.transform.forward;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale.Multiply(scalingVector);

        hc = this.EastChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position + GeometricChip.ChipSide*this.selectedChip.transform.right;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale.Multiply(scalingVector);

        hc = this.WestChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position - GeometricChip.ChipSide*this.selectedChip.transform.right;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale.Multiply(scalingVector);

        foreach (var c in this.HighlightCallbacks)
        {
            c(cc.equivalentVirtualChip);
        }
    }

    bool HighlightChip()
    {
        CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

        if (clickedObject is not null)
        {
            this.SelectVChip(clickedObject.equivalentVirtualChip.id);
            return true;
        }
        else
        {
            this.HighlightingChip.SetActive(false);
            this.NorthChip.SetActive(false);
            this.SouthChip.SetActive(false);
            this.EastChip.SetActive(false);
            this.WestChip.SetActive(false);
        }

        return false;
    }

}

