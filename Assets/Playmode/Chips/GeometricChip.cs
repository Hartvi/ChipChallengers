using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GeometricChip : StaticChip
{
    public CoreChip myCore = null;

    public GeometricChip parentChip = null;
    protected List<GeometricChip> childChips = new List<GeometricChip>();
    //public IReadOnlyList<GeometricChip> ChildChips => childChips;

    private VChip _equivalentVirtualChip;
    public VChip equivalentVirtualChip
    {
        get
        {
            return this._equivalentVirtualChip;
        }
        set
        {
            this._equivalentVirtualChip = value;
            value.rChip = (CommonChip)this;
        }
    }

    public bool IsCore { get { return this.equivalentVirtualChip.IsCore; } }

    private bool _VisualizePosition = false;
    private GameObject VisualizeSphere;
    public bool VisualizePosition
    {
        get
        {
            return this._VisualizePosition;
        }
        set
        {
            if (this.VisualizeSphere == null)
            {
                var sp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sp.GetComponent<MeshRenderer>().material.color = Color.red;
                GameObject.Destroy(sp.GetComponent<Collider>());
                sp.transform.localScale = new Vector3(1f / StaticChip.ChipSize.x, 1f / StaticChip.ChipSize.y, 1f / StaticChip.ChipSize.z) * StaticChip.ChipSide * 0.33f;
                sp.transform.SetParent(this.transform, false);
                this.VisualizeSphere = sp;

            }
            this._VisualizePosition = value;
            this.VisualizeSphere.SetActive(value);
        }
    }

    private Transform _inverse;

    public Transform inverse
    {
        get
        {
            if (_inverse == null)
            {
                var newInverse = new GameObject().transform;
                newInverse.name = StaticChip.inverseStr;
                // world position DOESNT stay because i want it locally at 0,0,0
                newInverse.SetParent(this.transform, false);
                var s = this.transform.localScale;
                newInverse.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                this._inverse = newInverse;
            }
            return this._inverse;
        }
    }

    protected int _option = -1;
    public int option { get { return this._option; } }

    public float mass
    {
        get
        {
            string chipType = this.equivalentVirtualChip.ChipType;

            switch (chipType)
            {
                // TODO: masses for each chip
                case VChip.chipStr:
                case VChip.rudderStr:
                case VChip.axleStr:
                    switch (this.option)
                    {
                        case 0:
                            { return PhysicsData.mediumMass; }
                        case 1:
                            { return PhysicsData.smallMass; }
                        case 2:
                            { return PhysicsData.largeMass; }
                        case 3:
                            { return PhysicsData.hugeMass; }
                        default: { return PhysicsData.mediumMass; }
                    }
                case VChip.wheelStr:
                    switch (this.option)
                    {
                        case 0:
                            { return PhysicsData.mediumMass; }
                        case 1:
                            { return 0.5f * (PhysicsData.mediumMass + PhysicsData.largeMass); }
                        case 2:
                            { return PhysicsData.largeMass; }
                        default: { return PhysicsData.mediumMass; }
                    }
                case VChip.jetStr:
                case VChip.gunStr:
                case VChip.coreStr:
                    if (this.option == 0)
                    {
                        return PhysicsData.mediumMass;
                    }
                    else if (this.option == 1)
                    {
                        return PhysicsData.smallMass;
                    }
                    else
                    {
                        //Debug.LogWarning($"Selecting unknown option `{this.option}` for chip `{this.equivalentVirtualChip.ChipType}`");
                        return PhysicsData.mediumMass;
                    }
                case VChip.cowlStr:
                case VChip.sensorStr:
                    return 0.1f;
                default:
                    return PhysicsData.mediumMass;
            }
        }
    }


    protected void SetupGeometry()
    {
        this.transform.localScale = GeometricChip.ChipSize;
    }

    public T Parent<T>() where T : GeometricChip
    {
        return (T)(this.parentChip);
    }

    public void SetChild(GeometricChip childChip)
    {
        // we don't want duplicates
        if (this.childChips.Contains(childChip))
        {
            throw new ArgumentException($"Chip {this} already has child {childChip}");
        }
        this.childChips.Add(childChip);
    }

    public void SetParent(GeometricChip parentChip)
    {
        // null parents not allowed in this function - we can only add Children to core and lower, not null
        if (this.parentChip != null)
        {
            throw new ArgumentException($"Parent of {this} must be null, cannot already have had a Parent.");
        }
        this.parentChip = parentChip;

        //print($"current Parent {parentChip.name} of {name}");
        parentChip.SetChild(this);
    }
}

