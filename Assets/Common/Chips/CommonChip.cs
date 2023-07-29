using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class CommonChip : AngleChip
{


    private Rigidbody _rb;
    protected Rigidbody rb
    {
        get
        {
            if (this._rb == null)
            {
                if (this.IsReal)
                {
                    this._rb = this.gameObject.AddComponent<Rigidbody>();
                }
                else
                {
                    throw new NullReferenceException($"Rigidbody of {this} is null.");
                }
            }

            return this._rb;
        }
    }

    protected ConfigurableJoint cj;

    private bool _IsReal = false;
    public bool IsReal
    {
        get
        {
            if (this._IsReal) return true;
            string chipType = this.equivalentVirtualChip.ChipType;
            if (this.IsJointElligible || chipType == VirtualChip.coreStr)
            {
                this._IsReal = true;
                return true;
            }
            return false;
        }
    }

    private bool _IsJointElligible = false;
    public bool IsJointElligible
    {
        get
        {
            // TODO REMOVE AND ADD OBJECT COMBINATION
            Debug.LogWarning($"TODO real and virtual chips");
            return true;
            return this.DetermineJointElligibility();
        }
    }

    private CommonChip _RealParent = null;
    public CommonChip RealParent
    {
        get
        {
            if (this._RealParent == null)
            {
                this._RealParent = this.GetHighestRealChip();
            }
            return this._RealParent;
        }
    }

    // FUNCTIONS:
    public List<CommonChip> AddChild(VirtualChip childChip)
    {
        var childType = childChip.ChipType;

        CommonChip newChild = GeometricChip.InstantiateChip<CommonChip>(childType);

        newChild.equivalentVirtualChip = childChip;
        newChild.SetParent(this);

        Vector3 newChildGlobalPosition = StaticChip.GetThisGlobalOffsetWrtParent(newChild);
        newChild.transform.position = newChildGlobalPosition;
        newChild.transform.rotation = this.transform.rotation;

        float angle = this.GetAngle();

        (Vector3 origin, Vector3 direction) = StaticChip.GetThisAxisOfRotationWrtParent(newChild);
        newChild.transform.RotateAround(origin, direction, angle);

        if (!newChild.IsJointElligible)
        {
            //print($"object {this} is not elligible for joint");
            newChild.transform.SetParent(this.inverse);
        }
        else
        {
            // TODO: create joint
            var parentRealChip = this.RealParent;
            //print($"parent object {parentRealChip}, current object: {this}");
            newChild.SetupGeometry();
            newChild.SetupRigidbody();
            ConfigurableJoint cj = JointUtility.AttachWithConfigurableJoint(
                newChild.gameObject, parentRealChip.gameObject, origin, direction
                );
            this.ConfigureJointAxis(cj);
        }
        var newChildren = newChild.AddChildren();
        // this is the changing part that fills up the list
        newChildren.Add(newChild);
        return newChildren;
    }

    public List<CommonChip> AddChildren()
    {
        List<CommonChip> chips = new List<CommonChip>();
        // when there are no children left then the recursion stops
        foreach (VirtualChip childChip in this.equivalentVirtualChip.children)
        {
            chips.AddRange(this.AddChild(childChip));
        }
        return chips;
    }

    private void ConfigureJointAxis(ConfigurableJoint cj)
    {
        this.cj = cj;
        int or = this.equivalentVirtualChip.orientation;
        switch (or)
        {
            case 0: cj.angularXMotion = ConfigurableJointMotion.Free; break;
            case 1: cj.angularZMotion = ConfigurableJointMotion.Free; break;
            case 2: cj.angularXMotion = ConfigurableJointMotion.Free; break;
            case 3: cj.angularZMotion = ConfigurableJointMotion.Free; break;
            default: throw new ArgumentException($"Orientation {or} is not in [0,3].");
        }
    }

    protected void SetupRigidbody()
    {
        if (this.rb == null)
        {
            throw new NullReferenceException($"Wanting to setup a null Rigidbody. Use a 'real' chip.");
        }
        if (this.equivalentVirtualChip == null)
        {
            throw new NullReferenceException($"To add Rigidbody you need an equivalent virtual chip.");
        }
        this.rb.mass = this.mass;
        this.rb.isKinematic = false;
        this.rb.drag = 0f;
        this.rb.angularDrag = 0f;
    }

    private CommonChip GetHighestRealChip()
    {
        CommonChip currentChip = this;

        var safeTrue = new SafeCode();

        while (safeTrue.Safe())
        {
            //print($"current chip: {currentChip}");
            // constant part
            if (currentChip.IsReal)
            {
                return currentChip;
            }
            // changing part
            currentChip = currentChip.Parent<CommonChip>();
        }
        throw new StackOverflowException($"Too many while true iterations.");
    }

    private bool DetermineJointElligibility()
    {
        // cached true, idempotent
        if (this._IsJointElligible) return true;

        string angleVal;
        if (!this.equivalentVirtualChip.TryGetProperty<string>(VirtualChip.angleStr, out angleVal))
        {
            this._IsJointElligible = false;
        }

        if (StringHelpers.IsVariableName(angleVal))
        {
            this._IsJointElligible = true;
            return this._IsJointElligible;
        }

        float springVal;
        bool springDamperExists = true;
        if (!this.equivalentVirtualChip.TryGetProperty<float>(VirtualChip.springStr, out springVal))
        {
            this._IsJointElligible = false;
            springDamperExists = false;
        }

        float damperVal;
        if (!this.equivalentVirtualChip.TryGetProperty<float>(VirtualChip.damperStr, out damperVal))
        {
            this._IsJointElligible = false;
            springDamperExists = false;
        }

        if (springVal >= 0f && damperVal >= 0f && springDamperExists)
        {
            this._IsJointElligible = true;
            return this._IsJointElligible;
        }

        return this._IsJointElligible;
    }

    public void TriggerSpawn(VirtualChip core)
    {
        this.equivalentVirtualChip = core;

        if (!this.equivalentVirtualChip.IsCore)
        {
            throw new ArgumentException($"Cannot trigger spawn from a non-core chip. (Current: {core.ChipType})");
        }

        this.SetupRigidbody();

        // this performs clean-up as well
        this.AllChildren = this.AddChildren();  // trigger the tsunami
        foreach (var c in this.AllChildren)
        {
            c.VisualizePosition = true;
        }
        this.VisualizePosition = true;
//        var newScript = new Script();
//        //        string codeStr = @"a = 1
//        //c = a + 5";
//        //        DynValue dyn = newScript.DoString(codeStr);
//        //        DynValue a = newScript.Globals.Get("a");
//        //        DynValue c = newScript.Globals.Get("c");
//        //        print($"dyn: {dyn}, a: {a.Number}, c: {c.Number}");
//        string codeStr = @"
//-- this is a comment loll
//chips = {
//            { id = 'aa', parentId = 'a', type = 'Core'},
//        { id = 'ab', parentId = 'a', type = 'Chip'}
//        }";
//        DynValue dyn = newScript.DoString(codeStr);
//        DynValue a = newScript.Globals.Get("chips");
//        //DynValue c = newScript.Globals.Get("c");
//        print($"dyn: {dyn}, a: ");
//        PRINT.print(((Table)a.Table[1]).Keys);
        var modelLua = this.VirtualModel.ToLuaString();
        IOHelpers.SaveTextFile("text.txt", modelLua);
        var modelLua2 = IOHelpers.LoadTextFile("text.txt");
        //print(modelLua);
        //var newScript = new Script();
        //DynValue a = newScript.DoString(modelLua);
        //print($"dyn: {a}, a: ");
        VirtualModel fromluadmodel = VirtualModel.FromLuaModel(modelLua2);
        //print(modelLua2);
    }



}

