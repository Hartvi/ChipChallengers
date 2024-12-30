using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using Mirror;
using System.Threading.Tasks;

public class CommonChip : AngleChip
{
    //[SyncVar]
    //public uint parentNetId = 0;
    [SyncVar]
    public string stringId = "a";

    [SyncVar]
    public uint myCoreNetId = 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(SrvWaitForCoreId());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        //if (!this.isServer)
        //{
        //    this.rb.isKinematic = true;
        //}
    }

    [Server]
    IEnumerator SrvWaitForCoreId()
    {
        // Wait until myCoreNetId is not 0
        while (this.myCoreNetId == 0)
        {
            yield return null; // Wait for the next frame
        }

        // Once myCoreNetId is set, proceed with the logic
        if (NetworkServer.spawned.TryGetValue(this.myCoreNetId, out var networkObject))
        {
            var cc = networkObject.GetComponent<CoreChip>();
            if (cc != null)
            {
                if (this.netId != this.myCoreNetId)
                {
                    cc.netIds = cc.netIds.Concat(new uint[] { this.netId }).ToArray();
                }
            }
            else
            {
                Debug.LogError("CoreChip component not found on the spawned object.");
            }
        }
        else
        {
            Debug.LogError($"No spawned object found with netId: {this.myCoreNetId}");
        }
    }

    //[Server]
    //IEnumerator WaitForParentChip()
    //{
    //    yield return new WaitUntil(() => (this.parentChip != null && this.myCoreNetId != 0) || this.IsCore);
    //    if (this.parentChip != null)
    //    {
    //        this.parentNetId = this.parentChip.netId;
    //        this.stringId = this.equivalentVirtualChip.id;
    //    }
    //    print($"srv: CHIP: {this.name}  ID: {this.netId}  PARENT NETID: {this.parentNetId}");
    //}

    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //StartCoroutine(WaitForParentId());
    //}

    //[Client]
    //IEnumerator WaitForParentId()
    //{
    //    yield return new WaitUntil(() => (this.parentNetId != 0 && this.myCoreNetId != 0) || this.IsCore);
    //    if (!this.IsCore)
    //    {
    //        var core = GameObject.FindObjectsOfType<CoreChip>().FirstOrDefault(x => x.netId == this.myCoreNetId);
    //        if (core.clientSideChips >= core.serverSideChips)
    //        {
    //            core.clientSideChips = 1;
    //        }
    //        core.clientSideChips++;
    //    }
    //}

#if UNITY_EDITOR
    static bool hasWarned = false;
    static bool hasWarned1 = false;
#endif

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
                this._rb = this.gameObject.GetComponent<Rigidbody>();
                if (this._rb == null)
                {
                    this._rb = this.gameObject.AddComponent<Rigidbody>();
                }
                else
                {
                    Debug.LogWarning($"Rigidbody has been added from the outside?");
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
            //TODO:
            return this.DetermineJointElligibility();
        }
    }

    bool isAeroElligible
    {
        get
        {
            VChip vc = this.equivalentVirtualChip;

            bool OptionAeroStuff = true;
            if (vc.TryGetProperty<int>(VChip.optionStr, out int option))
            {
                OptionAeroStuff = option < 1;
            }

            bool nonAeroType = new string[] { VChip.wheelStr, VChip.jetStr, VChip.cowlStr, VChip.sensorStr }.Contains(vc.ChipType);
            // option = 1 => false
            // option = 0 && cowl => false
            return !nonAeroType && OptionAeroStuff;
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
    [Client]
    public CommonChip[] AddChild(VChip childChip, CoreChip core)
    {
        var childType = childChip.ChipType;

        // TODO select the chip that matches the netId AND stringID
        CommonChip newChild = NetworkClient.spawned.First(x => core.netIds.Contains(x.Key) && x.Value.GetComponent<CommonChip>().stringId == childChip.id).Value.GetComponent<CommonChip>();

        newChild.equivalentVirtualChip = childChip;
        //print($"core: {core.netId}, {newChild.netId}: SETTING equivalent chip: {newChild.equivalentVirtualChip.rChip}");
        newChild.myCore = core;
        newChild.SetParent(this);

        Vector3 newChildGlobalPosition = StaticChip.GetThisGlobalOffsetWrtParent(newChild);
        newChild.transform.position = newChildGlobalPosition;
        newChild.transform.rotation = this.transform.rotation;

        // TODO fix get angle to take from `keys` and `values`
        float angle = newChild.GetAngle();

        (Vector3 origin, Vector3 direction) = StaticChip.GetThisAxisOfRotationWrtParent(newChild, VChip.chipNameToEnum[childChip.ChipType]);
        newChild.transform.RotateAround(origin, direction, angle);

        // rotates the objects so they are facing the correct direction
        Vector3 facingDirection = (newChild.transform.position - origin).normalized;

        float YrotationAngle = Vector3.Dot(facingDirection, newChild.transform.right);
        float YrotationAngle2 = Mathf.Max(0f, -Vector3.Dot(facingDirection, newChild.transform.forward));

        // apply transforms after measuring all information
        newChild.transform.RotateAround(newChild.transform.position, newChild.transform.up, 90f * YrotationAngle);
        newChild.transform.RotateAround(newChild.transform.position, newChild.transform.up, 180f * YrotationAngle2);

        if (!newChild.IsJointElligible)
        {
            newChild.transform.SetParent(this.inverse);
        }
        else
        {
            // TODO: create joint
            var parentRealChip = this.RealParent;

            newChild.SetupGeometry();
            //newChild.SetupRigidbody();
            ConfigurableJoint cj = JointUtility.AttachWithConfigurableJoint(
                newChild.gameObject, parentRealChip.gameObject, origin, direction
                );
            newChild.cj = cj;

            cj.angularXMotion = ConfigurableJointMotion.Free;
            newChild.ConfigureJointSpringDamper(cj);
        }


        var _newChildren = newChild.AddChildren(core);
        CommonChip[] newChildrenArr = new CommonChip[_newChildren.Length + 1];
        // this is the changing part that fills up the list
        _newChildren.CopyTo(newChildrenArr, 0);// (newChild);
        newChildrenArr[_newChildren.Length] = newChild;
        return newChildrenArr;
    }

    [Client]
    public CommonChip[] AddChildren(CoreChip core)
    {
        Color colour = this.GetColour();
        //print($"COLOUR: {colour}");
        this.mrs = this.GetComponentsInChildren<MeshRenderer>().Where(x => x.tag == VChip.colourStr).ToArray();
        this.materials = this.mrs.Select(x => x.material).ToArray();
        // to set the colour at build time
        foreach (Material m in this.materials)
        {
            m.color = colour;
        }

        int option = -1;
        if (this.equivalentVirtualChip.TryGetProperty<int>(VChip.optionStr, out option))
        {
            this._option = option;
            this.SelectOption(option);
        }
        this.SetupRigidbody();

        // add runtime aspects
        if (this.isAeroElligible && this.isServer)
        {
            // add aspects, TODO: this wont add it to core!!!
            this.gameObject.AddComponentIdempotent<Aerodynamics>().myChip = this;
        }

        if (this.equivalentVirtualChip.HasHealth())
        {
            HealthAspect h = this.gameObject.AddComponentIdempotent<HealthAspect>();
            h.SetDeathCallbacks(new Action[] { this.Die });
            h.SetHealth(this.equivalentVirtualChip.DefaultHealth());
        }

        if (this.equivalentVirtualChip.ChipType == VChip.cowlStr)
        {
            this.gameObject.AddComponentIdempotent<CowlAspect>().myChip = this;
        }
        else
        {
            this.gameObject.AddComponentIdempotent<DustAspect>().myChip = this;
            this.gameObject.AddComponentIdempotent<HitSoundAspect>().myChip = this;
        }

        if (this.equivalentVirtualChip.ChipType == VChip.sensorStr && !this.isServer)
        {
            this.gameObject.AddComponentIdempotent<SensorAspect>().myChip = this;
        }

        if (this.equivalentVirtualChip.keys.Contains(VChip.valueStr))
        {

            // TODO cosmetics as in jet spitting fire and wheel turning discs
            this._value = this.GetValue();

            if (this.equivalentVirtualChip.ChipType == VChip.jetStr)
            {
                this.gameObject.AddComponentIdempotent<JetAspect>();
                this.gameObject.AddComponentIdempotent<JetSoundAspect>();
                this.gameObject.AddComponentIdempotent<JetDustAspect>();
                this.gameObject.AddComponentIdempotent<JetFlameAspect>();
            }
            if (this.equivalentVirtualChip.keys.Contains(VChip.brakeStr))
            {
                this._brake = this.GetBrake();
                // GUN - power = gun power, brake to trigger,
                // WHEEL - power = power, brake = brake
                if (this.equivalentVirtualChip.ChipType == VChip.wheelStr)
                {
                    this.gameObject.AddComponentIdempotent<WheelAspects>().myChip = this;
                    this.gameObject.AddComponentIdempotent<WheelSoundAspect>().myChip = this;
                    //this.gameObject.AddComponentIdempotent<TireSoundAspect>().myChip = this;
                }
                if (this.equivalentVirtualChip.ChipType == VChip.gunStr)
                {
                    //if(this.isServer) 
                    this.gameObject.AddComponentIdempotent<GunAspect>().myChip = this;
                    this.gameObject.AddComponentIdempotent<GunSoundAspect>().myChip = this;
                    this.gameObject.AddComponentIdempotent<GunDustAspect>().myChip = this;
                }
            }
        }

        List<CommonChip> allChildren = new List<CommonChip>();
        // when there are no Children left then the recursion stops
        foreach (VChip childChip in this.equivalentVirtualChip.Children)
        {
            allChildren.AddRange(this.AddChild(childChip, core));
        }
        return allChildren.ToArray();
    }

    private void ConfigureJointSpringDamper(ConfigurableJoint cj)
    {
        // spring damper
        // Don't mind default since I don't care
        float spring;
        this.equivalentVirtualChip.TryGetProperty<float>(VChip.springStr, out spring);
        float damper;
        this.equivalentVirtualChip.TryGetProperty<float>(VChip.damperStr, out damper);

        JointDrive jd = new JointDrive();
        jd.positionSpring = spring;
        jd.positionDamper = damper;
        jd.maximumForce = Mathf.Max(spring, damper);
        cj.angularXDrive = jd;

        float defaultDamper = (float)ArrayExtensions.AccessLikeDict(VChip.damperStr, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);
        float defaultSpring = (float)ArrayExtensions.AccessLikeDict(VChip.springStr, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);

        //if (spring >= defaultSpring && damper >= defaultDamper)
        //{
        //    cj.angularXMotion = ConfigurableJointMotion.Locked;
        //    cj.angularZMotion = ConfigurableJointMotion.Locked;
        //}
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


    public void Die()
    {
        GameObject.Destroy(this.cj);

        CommonChip[] myDescendants = this.GetMyDescendants();

        foreach (CommonChip descendant in myDescendants)
        {
            descendant.SleepyTime();
        }
    }

    private void SleepyTime()
    {
        // remove from callbacks to LUA
        this.RemoveMeFromVariableCallbacks.Invoke();
    }

    CommonChip[] GetMyDescendants()
    {
        List<CommonChip> myDescendants = new List<CommonChip>();
        myDescendants.Add(this);
        /*
         a-(b,c-(d,e,f))
        add b
        add descendants of b => nothing
        add c
        add descendants of c =>
         add d
         add e
         add f
         */
        for (int i = 0; i < this.childChips.Count; ++i)
        {
            CommonChip myChild = this.childChips[i] as CommonChip;
            myDescendants.Add(myChild);
            myDescendants.AddRange(myChild.GetMyDescendants());
        }
        return myDescendants.ToArray();
    }

}

