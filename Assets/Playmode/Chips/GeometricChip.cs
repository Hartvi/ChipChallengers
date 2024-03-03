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
    public GeometricChip[] AllChips
    {
        get
        {
            return this.AllChildren.Concat(new[] { this }).ToArray();
        }
    }

    //public VChip[] AllVirtualChips
    //{
    //    get
    //    {
    //        if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access all virtual chips from a non-core object.");

    //        var virtualChips = this.AllChips.Select(child => child.equivalentVirtualChip).ToArray();
    //        return virtualChips;
    //    }
    //}

    public VVar[] AllVirtualVariables
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access all virtual variables from a non-core object.");
            //return this.VirtualVariables.ToArray();
            return this.VirtualModel.variables;
        }
    }

    //protected string script;

    private VModel _VirtualModel;
    public VModel VirtualModel
    {
        get
        {
            if (!this.IsCore) throw new FieldAccessException("Trying to access virtual model from a non-core object.");
            if(this._VirtualModel == null)
            {
                throw new NullReferenceException($"Virtual model of core is null.");
                //this._VirtualModel = new VirtualModel();
            }
            // taking chips away goes through real chips
            // adding chips goes through VirtualModel
            //this._VirtualModel.chips = this.AllVirtualChips;
            //this._VirtualModel.variables = this.AllVirtualVariables;
            //this._VirtualModel.script = this.script ?? "";
            return this._VirtualModel;
        }
        set
        {
            //print($"set virtual model");
            this._VirtualModel = value;
            this.equivalentVirtualChip = value.Core;
        }
    }

    private CommonChip[] _AllChildren;
    public CommonChip[] AllChildren
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore)
            {
                throw new MemberAccessException($"Get: Only chip designated as core can access all Children.");
            }
            return this._AllChildren;
        }
        set
        {
            if (!this.equivalentVirtualChip.IsCore)
            {
                throw new MemberAccessException($"Set: Only chip designated as core can access all Children.");
            }
            if (this._AllChildren is not null)
            {
                foreach (CommonChip child in this._AllChildren)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
            //this._AllChildren.Clear();
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
    protected int _option = -1;
    protected int option
    {
        get
        {
            if (this._option == -1)
            {
                throw new InvalidOperationException($"Mass depends on option. Option has not been set yet.");
            }
            return this._option;
        }
        set { this._option = value; }
    }

    public float mass
    {
        get
        {
            string chipType = this.equivalentVirtualChip.ChipType;
            this.equivalentVirtualChip.TryGetProperty<int>(VChip.optionStr, out int option);
            this.option = option;

            switch (chipType)
            {
                // TODO: masses for each chip
                case VChip.wheelStr:
                case VChip.jetStr:
                case VChip.rudderStr:
                case VChip.chipStr:
                case VChip.axleStr:
                case VChip.gunStr:
                case VChip.coreStr:
                case VChip.sensorStr:
                    if (this.option == 0)
                    {
                        return PhysicsData.mediumMass;
                    } else if(this.option == 1)
                    {
                        return PhysicsData.smallMass;
                    } else
                    {
                        Debug.LogWarning($"Selecting unknown option `{this.option}` for chip `{this.equivalentVirtualChip.ChipType}`");
                        return PhysicsData.mediumMass;
                    }
                case VChip.cowlStr:
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

