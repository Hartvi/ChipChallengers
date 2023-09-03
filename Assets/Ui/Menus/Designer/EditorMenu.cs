using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorMenu : BaseMenu
{
    private Camera _camera;
    private GeometricChip selectedChip;

    private GameObject HighlightingChip, NorthChip, SouthChip, EastChip, WestChip;
    private Action<VChip>[] HighlightCallbacks = new Action<VChip>[] { };

    public CreateChipPanel CreateChipPanel;

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, left,
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
        );
    }

    protected override void Start()
    {
        this.CreateChipPanel = this.GetComponentInChildren<CreateChipPanel>();

        this.AddSelectionCallback(CommonChip.FreezeModel);

        foreach(var c in this.selectedCallbacks)
        {
            c();
        }
        base.Start();

        this._camera = Camera.main;

        this.HighlightingChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.HighlightingChip.GetComponent<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.8f, 0.1f);
        this.HighlightingChip.SetActive(false);

        GameObject.Destroy(this.HighlightingChip.GetComponent<Collider>());

        this.NorthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ColourHighlighter ch = this.NorthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.North;

        this.NorthChip.GetComponent<MeshRenderer>().material.color = new Color(0.8f, 0.0f, 0.0f, 0.1f);
        this.NorthChip.SetActive(false);

        this.SouthChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.SouthChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.South;

        this.SouthChip.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 0.0f, 0.8f, 0.1f);
        this.SouthChip.SetActive(false);

        this.EastChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.EastChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.East;

        this.EastChip.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 0.8f, 0.0f, 0.1f);
        this.EastChip.SetActive(false);

        this.WestChip = GameObject.CreatePrimitive(PrimitiveType.Cube);

        ch = this.WestChip.AddComponent<ColourHighlighter>();
        ch.localDirection = LocalDirection.West;

        this.WestChip.GetComponent<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.0f, 0.1f);
        this.WestChip.SetActive(false);
    }

    public VChip selectedVChip
    {
        get
        {
            if (this.selectedChip is not null)
            {
                return selectedChip.equivalentVirtualChip;
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
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.gameObject.GetComponent<T>();
        }
        
        return default(T);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !TopUI.IsOnUI)  // 0 means left mouse button
        {
            // if not adding new chip, then try highlighting chip
            if (!this.DisplayChipMenu())
            {
                this.HighlightChip();
            }
            
        }
    }

    bool DisplayChipMenu()
    {
        ColourHighlighter clickedObject = this.GetObjectFromScreenClick<ColourHighlighter>();

        if (clickedObject is not null)
        {
            CreateChipPanel.Display(Input.mousePosition, clickedObject.localDirection, selectedVChip);
            print($"clicked direction: {clickedObject.localDirection}");
            return true;   
        }
        CreateChipPanel.gameObject.SetActive(false);
        return false;
    }

    public void SelectVChip(CommonChip cc)
    {
        //print($"Selected chip type: {cc.equivalentVirtualChip.ChipType}.");
        this.selectedChip = cc;

        var hc = this.HighlightingChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = 1.1f * this.selectedChip.transform.localScale;

        hc = this.NorthChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position + GeometricChip.ChipSide*this.selectedChip.transform.forward;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale;

        hc = this.SouthChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position - GeometricChip.ChipSide*this.selectedChip.transform.forward;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale;

        hc = this.EastChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position + GeometricChip.ChipSide*this.selectedChip.transform.right;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale;

        hc = this.WestChip;
        hc.SetActive(true);
        hc.transform.position = this.selectedChip.transform.position - GeometricChip.ChipSide*this.selectedChip.transform.right;
        hc.transform.rotation = this.selectedChip.transform.rotation;
        hc.transform.localScale = this.selectedChip.transform.localScale;

        foreach(var c in this.HighlightCallbacks)
        {
            c(cc.equivalentVirtualChip);
        }
    }

    bool HighlightChip()
    {
        CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

        if (clickedObject is not null)
        {
            this.SelectVChip(clickedObject);
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

