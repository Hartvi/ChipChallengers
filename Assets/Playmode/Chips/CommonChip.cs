using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class CommonChip : AngleChip
{
#if UNITY_EDITOR
    static bool hasWarned = false;
    static bool hasWarned1 = false;
#endif
    public ScriptInstance scriptInstance;

    private LoopScript loopScript;
    public static CommonChip ClientCore {
        get
        {
            foreach(var o in GameObject.FindObjectsOfType<CommonChip>())
            {
                if(o.IsOnClient && o.equivalentVirtualChip.IsCore)
                {
                    return o;
                }
            }
            throw new NullReferenceException($"Couldn't find client's core.");
        }
    }

    public bool IsOnClient
    {
        get
        {
#if UNITY_EDITOR
            if (!CommonChip.hasWarned1)
            {
                CommonChip.hasWarned1 = true;
                Debug.LogWarning($"TODO check if it's on client in multiplayer.");
            }
#endif
            return true;
        }
    }

    public bool IsFocusable
    {
        get
        {
            UnityEngine.Debug.LogWarning("IsFocusable not fully implemented, TODO: if it's a real chip.");
            return this.equivalentVirtualChip.IsCore;
        }
    }

    private Rigidbody _rb;
    public Rigidbody rb
    {
        get
        {
            if (this._rb == null)
            {
                if (this.IsReal)
                {
                    this._rb = this.gameObject.GetComponent<Rigidbody>();
                    if(this._rb == null)
                    {
                        this._rb = this.gameObject.AddComponent<Rigidbody>();
                    }
                    else
                    {
                        Debug.LogWarning($"Rigidbody has been added from the outside?");
                    }
                }
                else
                {
                    throw new NullReferenceException($"Rigidbody of {this} is null.");
                }
            }

            return this._rb;
        }
    }


    private bool _IsReal = false;
    public bool IsReal
    {
        get
        {
            if (this._IsReal) return true;
            string chipType = this.equivalentVirtualChip.ChipType;
            if (this.IsJointElligible || chipType == VChip.coreStr)
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
#if UNITY_EDITOR
            if (!CommonChip.hasWarned)
            {
                CommonChip.hasWarned = true;
                Debug.LogWarning($"TODO real and virtual chips");
            }
#endif
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
    Action[] _AfterBuildActions = new Action[] { };

    // FUNCTIONS:
    public CommonChip[] AddChild(VChip childChip)
    {
        var childType = childChip.ChipType;

        CommonChip newChild = GeometricChip.InstantiateChip<CommonChip>(childType);
        //print($"new child: {newChild}, id: {childChip.id}");

        newChild.equivalentVirtualChip = childChip;
        newChild.SetParent(this);

        Vector3 newChildGlobalPosition = StaticChip.GetThisGlobalOffsetWrtParent(newChild);
        newChild.transform.position = newChildGlobalPosition;
        newChild.transform.rotation = this.transform.rotation;

        // TODO fix get angle to take from `keys` and `values`
        float angle = newChild.GetAngle();
        //print($"chip: {childChip.ChipType}, angle: {angle}, child angle: {childChip.keys[0]}");

        (Vector3 origin, Vector3 direction) = StaticChip.GetThisAxisOfRotationWrtParent(newChild, VChip.chipNameToEnum[childChip.ChipType]);
        newChild.transform.RotateAround(origin, direction, angle);

        float YrotationAngle = Vector3.Dot((origin - newChild.transform.position).normalized, -newChild.transform.right);
        newChild.transform.RotateAround(newChild.transform.position, newChild.transform.up, 90f*YrotationAngle);

        if (!newChild.IsJointElligible)
        {
            //print($"object {this} is not elligible for joint");
            newChild.transform.SetParent(this.inverse);
        }
        else
        {
            // TODO: create joint
            var parentRealChip = this.RealParent;
            //print($"Parent object {parentRealChip}, current object: {this}");
            newChild.SetupGeometry();
            newChild.SetupRigidbody();
            ConfigurableJoint cj = JointUtility.AttachWithConfigurableJoint(
                newChild.gameObject, parentRealChip.gameObject, origin, direction
                );
            newChild.cj = cj;
            //print($"Just set joint: {newChild.equivalentVirtualChip.id}");
            // TODO: position mode for axle AND velocity mode for axle => spring/damper
            //cj.targetRotation = Quaternion.Euler(20, 0, 0);
            this.ConfigureJointAxis(cj);
            newChild.ConfigureJointSpringDamper(cj);

            // set target rotation from `GetAngle`
            //cj.targetRotation = this.targetRotation;
        }

        var _newChildren = newChild.AddChildren();
        CommonChip[] newChildrenArr = new CommonChip[_newChildren.Length + 1];
        // this is the changing part that fills up the list
        _newChildren.CopyTo(newChildrenArr, 0);// (newChild);
        newChildrenArr[_newChildren.Length] = newChild;
        return newChildrenArr;
    }

    public CommonChip[] AddChildren()
    {
        Color colour = this.GetColour();
        this.mr = this.GetComponentInChildren<MeshRenderer>();
        this.material = this.mr.material;
        // to set the colour at build time
        this.material.color = colour;

        List<CommonChip> chips = new List<CommonChip>();

        // when there are no Children left then the recursion stops
        //print($"Number of children: {this.equivalentVirtualChip.children.Count}");
        foreach (VChip childChip in this.equivalentVirtualChip.Children)
        {
            chips.AddRange(this.AddChild(childChip));
        }
        return chips.ToArray();
    }

    private void ConfigureJointSpringDamper(ConfigurableJoint cj)
    {
        // spring damper
        float spring = this.equivalentVirtualChip.GetPropertyOrDefault<float>(VChip.springStr);
        float damper = this.equivalentVirtualChip.GetPropertyOrDefault<float>(VChip.damperStr);
        //print($"chip: {this.equivalentVirtualChip.ChipType}: spring: {spring} damper: {damper}");

        JointDrive jd = new JointDrive();
        jd.positionSpring = spring;
        jd.positionDamper = damper;
        jd.maximumForce = Mathf.Max(spring, damper);

        if(cj.angularXMotion == ConfigurableJointMotion.Free)
        {
            cj.angularXDrive = jd;
        }
        if(cj.angularZMotion == ConfigurableJointMotion.Free)
        {
            cj.angularYZDrive = jd;
        }

        float defaultDamper = (float)ArrayExtensions.AccessLikeDict(VChip.damperStr, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);
        float defaultSpring = (float)ArrayExtensions.AccessLikeDict(VChip.springStr, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);

        if (spring >= defaultSpring && damper >= defaultDamper)
        {
            cj.angularXMotion = ConfigurableJointMotion.Locked;
            cj.angularZMotion = ConfigurableJointMotion.Locked;
        }
    }
    private void ConfigureJointAxis(ConfigurableJoint cj)
    {
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
        if (!this.equivalentVirtualChip.TryGetProperty<string>(VChip.angleStr, out angleVal))
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
        if (!this.equivalentVirtualChip.TryGetProperty<float>(VChip.springStr, out springVal))
        {
            this._IsJointElligible = false;
            springDamperExists = false;
        }

        float damperVal;
        if (!this.equivalentVirtualChip.TryGetProperty<float>(VChip.damperStr, out damperVal))
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

    public void TriggerSpawn(VModel virtualModel, bool freeze)
    {
        print("TRIGGER SPAWN");
        this.VirtualModel = virtualModel;
        foreach(VVar v in this.VirtualModel.variables)
        {
            v.valueChangedCallbacks = new Action<float, VVar>[] { };
        }

        this.transform.localScale = StaticChip.ChipSize;

        // this should replace the argument
        VChip core = this.VirtualModel.Core;

        this.equivalentVirtualChip = core;

        if (!this.equivalentVirtualChip.IsCore)
        {
            throw new ArgumentException($"Cannot trigger spawn from a non-core chip. (Current: {core.ChipType})");
        }

        this.SetupRigidbody();

        // handle script
        this.scriptInstance = new ScriptInstance(virtualModel);

        if(this.loopScript is not null) GameObject.Destroy(this.loopScript);

        this.loopScript = this.gameObject.AddComponent<LoopScript>();
        this.loopScript.loopFunction = this.scriptInstance.CallLoop;

        // this performs clean-up as well
        this.AllChildren = this.AddChildren();  // trigger the tsunami

        foreach (var c in this.AllChildren)
        {
            c.VisualizePosition = true;
        }
        this.VisualizePosition = true;

        //c.transform.position = c.transform.position + Vector3.up;
        if (freeze)
        {
            CommonChip.FreezeModel();
        }
        foreach(var a in this._AfterBuildActions)
        {
            //print("after build action");
            a();
        }
    }

    public static void FreezeModel()
    {
        var c = CommonChip.ClientCore;

        foreach(GeometricChip chip in c.AllChips)
        {
            chip.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void SetAfterBuildListeners(Action[] actions)
    {
        this._AfterBuildActions = actions;
    }

    void Update()
    {
        //if(this.cj != null)
        //print($"target rotation: {this.targetRotation}, joint: {this.cj.targetRotation}");
        //if (this.cj == null)
        //    print($"Joint is null");
    }
}

