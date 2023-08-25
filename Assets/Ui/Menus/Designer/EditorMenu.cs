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

    public GameObject GetObjectFromScreenClick()
    {
        if (Input.GetMouseButtonDown(0))  // 0 means left mouse button
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                return hit.collider.gameObject;
            }
        }
        return null;
    }


    void Update()
    {
        GameObject clickedObject = this.GetObjectFromScreenClick();

        if (clickedObject is not null)
        {
            this.selectedChip = clickedObject.GetComponent<GeometricChip>();
            print(this.selectedChip);
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

