using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorMenu : BaseMenu
{
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
            new VirtualProp(PropType.Panel, -1f
            )
        );
    }

    protected override void Start()
    {
        this.AddSelectionCallback(this.FreezeModel);
        foreach(var c in this.selectedCallbacks)
        {
            c();
        }
        base.Start();

        _camera = Camera.main;

        this.HighlightingChip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject.Destroy(this.HighlightingChip.GetComponent<Collider>());
        this.HighlightingChip.GetComponent<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.8f, 0.1f);
        this.HighlightingChip.SetActive(false);
    }

    private Camera _camera;
    private GeometricChip selectedChip;
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

    private GameObject HighlightingChip;
    private Action<VChip>[] HighlightCallbacks = new Action<VChip>[] { };

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
        if (Input.GetMouseButtonDown(0))  // 0 means left mouse button
        {
            this.HighlightChip();
        }
    }

    void HighlightChip()
    {
        CommonChip clickedObject = this.GetObjectFromScreenClick<CommonChip>();

        if (clickedObject is not null)
        {
            this.selectedChip = clickedObject;
            var hc = this.HighlightingChip;
            hc.SetActive(true);
            hc.transform.position = this.selectedChip.transform.position;
            hc.transform.rotation = this.selectedChip.transform.rotation;
            hc.transform.localScale = 1.1f * this.selectedChip.transform.localScale;

            foreach(var c in this.HighlightCallbacks)
            {
                c(clickedObject.equivalentVirtualChip);
            }
        }
        else
        {
            this.HighlightingChip.SetActive(false);
        }
    }

    void FreezeModel()
    {
        var c = CommonChip.ClientCore;
        c.transform.position = c.transform.position + Vector3.up;
        c.TriggerSpawn(c.VirtualModel);

        foreach(var chip in c.AllChips)
        {
            chip.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

}

