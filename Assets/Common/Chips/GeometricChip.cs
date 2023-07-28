using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GeometricChip : StaticChip
{
    public GeometricChip parentChip = null;
    protected List<GeometricChip> childChips = new List<GeometricChip>();
    //public IReadOnlyList<GeometricChip> ChildChips => childChips;

    public VirtualChip equivalentVirtualChip;

    public GeometricChip[] AllChips
    {
        get
        {
            return this.AllChildren.Concat(new[] { this }).ToArray();
        }
    }

    public VirtualChip[] AllVirtualChips
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access all virtual chips from a non-core object.");
            var virtualChips = this.AllChips.Select(child => child.equivalentVirtualChip).ToArray();
            return virtualChips;
        }
    }

    private List<VirtualVariable> _VirtualVariables;
    protected List<VirtualVariable> VirtualVariables { 
        get
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access virtual variables from a non-core object.");
            if(this._VirtualVariables == null) { return new List<VirtualVariable>(); }
            return this._VirtualVariables;
        }
        set
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access virtual variables from a non-core object.");
            this._VirtualVariables = value;
        }
    }

    public VirtualVariable[] AllVirtualVariables
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access all virtual chips from a non-core object.");
            return this.VirtualVariables.ToArray();
        }
    }

    protected string script;

    public VirtualModel VirtualModel
    {
        get
        {
            var newModel = new VirtualModel();
            newModel.chips = this.AllVirtualChips;
            newModel.variables = this.AllVirtualVariables;
            newModel.script = this.script ?? "";
            return newModel;
        }
    }

    private List<CommonChip> _AllChildren = new List<CommonChip>();
    public List<CommonChip> AllChildren
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore)
            {
                throw new MemberAccessException($"Get: Only chip designated as core can access all children.");
            }
            return this._AllChildren;
        }
        set
        {
            if (!this.equivalentVirtualChip.IsCore)
            {
                throw new MemberAccessException($"Set: Only chip designated as core can access all children.");
            }
            foreach (CommonChip child in this._AllChildren)
            {
                GameObject.Destroy(child.gameObject);
            }
            this._AllChildren.Clear();
            this._AllChildren = value;
        }
    }

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

    public float mass
    {
        get
        {
            string chipType = this.equivalentVirtualChip.ChipType;
            switch (chipType)
            {
                // TODO: masses for each chip
                default: return 1f;
            }
        }
    }


    protected void SetupGeometry()
    {
        this.transform.localScale = GeometricChip.ChipSize;
    }

    public T Parent<T>() where T : GeometricChip
    {
        return (T)(parentChip);
    }

    public void SetChild(GeometricChip childChip)
    {
        // we don't want duplicates
        if (childChips.Contains(childChip))
        {
            throw new ArgumentException($"Chip {this} already has child {childChip}");
        }
        childChips.Add(childChip);
    }

    public void SetParent(GeometricChip parentChip)
    {
        // null parents not allowed in this function - we can only add children to core and lower, not null
        if (this.parentChip != null)
        {
            throw new ArgumentException($"Parent of {this} must be null, cannot already have had a parent.");
        }
        this.parentChip = parentChip;

        //print($"current parent {parentChip.name} of {name}");
        parentChip.SetChild(this);
    }
}

