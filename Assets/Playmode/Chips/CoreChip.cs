using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoreChip : CommonChip
{
    public List<BaseAspect> RuntimeFunctions = new List<BaseAspect>();

    [SyncVar]
    public string modelString = null;

    public static NetworkIdentity ClientCore => NetworkClient.localPlayer;
    public static CoreChip ClientCoreChip => NetworkClient.localPlayer.GetComponent<CoreChip>();

    Action[] _AfterBuildActions = new Action[] { };
    Action[] OnLoadedCallbacks = { };
    public HistoryStack history = new HistoryStack();
    public ScriptInstance scriptInstance;

    private LoopScript loopScript;

    public GeometricChip[] AllChips
    {
        get
        {
            Debug.Assert(this.isServer);
            return this.AllChildren.Concat(new[] { this }).ToArray();
        }
    }

    public VVar[] AllVirtualVariables
    {
        get
        {
            if (!this.equivalentVirtualChip.IsCore) throw new FieldAccessException("Trying to access all virtual variables from a non-core object.");
            //return this.VirtualVariables.ToArray();
            return this.VirtualModel.variables;
        }
    }

    private VModel _VirtualModel;
    public VModel VirtualModel
    {
        get
        {
            if (!this.IsCore) throw new FieldAccessException("Trying to access virtual model from a non-core object.");
            if (this.isClientOnly)
            {
                return VModel.FromLuaModel(this.modelString);
            }
            else if (this._VirtualModel == null)
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
            if (this.isServer)
            {
                this.modelString = value.ToLuaString();
            }
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
                    if (child && child.gameObject)
                    {
                        GameObject.Destroy(child.gameObject);
                    }
                }
            }
            //this._AllChildren.Clear();
            this._AllChildren = value;
        }
    }

    void Awake()
    {
        string[] keys1 = new string[] { VChip.nameStr, VChip.typeStr };
        string[] vals1 = new string[] { VChip.coreStr, VChip.coreStr };
        this.equivalentVirtualChip = new VChip(keys1, vals1, 0, null);
    }

    public override void OnStartClient()
    {
        // THIS IS SO JOINTS WORK ON THE SERVER
        // THE CLIENT WILL SEND CONTROL COMMANDS TO THE SERVER WHICH WILL THEN ACT ON THEM
        base.OnStartClient();
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();
        print($"srv: OnStartServer");

        TextAsset textFile = Resources.Load<TextAsset>("aguncar");
        this.LoadString(textFile.text);

        Action[] onLoadedCallbacksTmp = new Action[] {
            () => {
                this.TriggerSpawn(this.VirtualModel, false);
                this.transform.position += Vector3.up;
                }
            //() => this.Hud.LinkCore(this),
            //() => {
            //    Camera.main.transform.position = this.transform.position + Vector3.up * 10f;
            //}
        };
        this.OnLoadedCallbacks = onLoadedCallbacksTmp;
    }

    void Update()
    {
    }

    [Command]
    public void CmdLoadString(string state)
    {
        print($"Building core: {this.netId}");
        this.RuntimeFunctions.Clear();
        this.LoadString(state);
    }

    [Server]
    public void LoadString(string state)
    {
        print($"srv: LoadString");
        VModel model = null;
        try
        {
            model = VModel.FromLuaModel(state);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"string model could not be loaded.");
            UnityEngine.Debug.Log(e.Message);
            // TODO send debug message to client
            DisplaySingleton.Instance.DisplayText(LoadPanel.UndoRedoNotValid, 3f);

            return;
        }

        this.TriggerSpawn(model, true);
        this.VirtualModel.AddModelChangedCallback(x => this.TriggerSpawn(x, true));
        this.VirtualModel.AddModelChangedCallback(x => this.history.SaveState(this.VirtualModel.ToLuaString()));
        //this.history.SaveState(this.VirtualModel.ToLuaString());

        foreach (Action a in this.OnLoadedCallbacks)
        {
            a();
        }
    }

    [Command]
    public void CmdUndoHistory()
    {
        print($"clt=>srv");
        this.LoadString(this.history.Undo());
    }

    [Command]
    public void CmdRedoHistory()
    {
        print($"clt=>srv");
        this.LoadString(this.history.Redo());
    }

    [Command]
    public void CmdTriggerSpawn()
    {
        this.TriggerSpawn(this.VirtualModel, false);
    }

    [Command]
    public void CmdResetCore()
    {
        print($"clt=>srv: CmdResetCore");
        // delete after build listeners
        this.RpcResetRotationVelocity();
        this.SetAfterBuildListeners(new Action[] { });
        this.RpcRetrigger();
    }

    [Command]
    public void CmdResetToDefaultLocation()
    {
        print($"clt=>srv: CmdResetToDefaultLocation");
        this.RpcResetLocation();
        this.RpcResetRotationVelocity();
        this.SetAfterBuildListeners(new Action[] { });
        this.RpcRetrigger();
    }

    [ClientRpc]
    public void RpcResetLocation()
    {
        Vector3 spawnPosition = StaticChip.RaycastFromAbove();
        this.transform.position = spawnPosition;
    }

    [ClientRpc]
    public void RpcResetRotationVelocity()
    {
        this.rb.velocity = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
    }

    [ClientRpc]
    public void RpcRetrigger()
    {
        this.CmdTriggerSpawn();
    }

    [Server]
    public void TriggerSpawn(VModel virtualModel, bool freeze)
    {
        print($"srv: TriggerSpawn");
        Debug.Assert(!this.isClientOnly);
        Debug.Assert(this.isServer);

        var ni = this.GetComponent<NetworkIdentity>();
        if (this.equivalentVirtualChip.ChipType != VChip.coreStr)
        {
            throw new InvalidOperationException($"Attempting to TriggerSpawn on non-core chip: {this.equivalentVirtualChip.ChipType}!");
        }
        if (ni.netId == 0)
        {
            throw new NullReferenceException($"Attempting to TriggerSpawn on offline chip!");
        }
        if (!ni.isServer)
        {
            throw new AccessViolationException($"TriggerSpawn must be called only on server!");
        }
        this.RuntimeFunctions.Clear();
        this.VirtualModel = virtualModel;

        foreach (VVar v in this.VirtualModel.variables)
        {
            v.valueChangedCallbacks = new Action<float, VVar>[] { };
            v.currentValue = v.defaultValue;
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

        if (this.loopScript is not null)
        {
            Debug.LogWarning($"Loop script is being added twice, deleting old one");
            GameObject.Destroy(this.loopScript);
        }

        this.loopScript = this.gameObject.AddComponentIdempotent<LoopScript>();
        this.loopScript.vModel = this.VirtualModel;
        this.loopScript.loopFunction = this.scriptInstance.CallLoop;


        // this performs clean-up as well
        this.myCore = this;
        this.AllChildren = this.AddChildren(this);  // trigger the tsunami
        foreach (var child in this.AllChildren)
        {
            var tmpNi = child.GetComponent<NetworkIdentity>();
            NetworkServer.Spawn(child.gameObject);
        }
        // TODO: remove this and FIX Clipboard
        if (this.VirtualModel.chips.Length != this.AllChips.Length)
        {
            Debug.LogWarning($"Fix clipboard to get rid of this warning");
            // this is to register chips that haven't been added in
            this.VirtualModel.SetChipsWithoutNotify(this.AllChips.Select(x => x.equivalentVirtualChip).ToArray());
        }
        this.scriptInstance.LinkSensors(this.VirtualModel);


        //foreach (var c in this.AllChildren)
        //{
        //    c.VisualizePosition = true;
        //}
        //this.VisualizePosition = true;

        this.SrvFreezeClientModel();
        if (freeze)
        {
            this.SrvFreezeClientModel();
        }
        foreach (var a in this._AfterBuildActions)
        {
            a();
        }
    }

    [Command]
    public void CmdUnfreezeClientModel()
    {
        if (!this.IsCore)
        {
            DisplaySingleton.Instance.DisplayText(x =>
            {
                x.SetText($"UnfreezeClientModel: {this.name} isn't a core!");
            }, 3f);
        }

        foreach (GeometricChip chip in this.AllChips)
        {
            chip.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    [Server]
    public void SrvFreezeClientModel()
    {
        print($"srv: FreeClientModel");
        Debug.Assert(!this.isClientOnly);
        Debug.Assert(this.isServer);
        if (!this.IsCore)
        {
            DisplaySingleton.Instance.DisplayText(x =>
            {
                x.SetText($"FreezeClientModel: {this.name} isn't a core!");
            }, 3f);
        }

        foreach (GeometricChip chip in this.AllChips)
        {
            var r = chip.GetComponent<Rigidbody>();
            if (r != null)
            {
                chip.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    [Command]
    public void CmdFreezeClientModel()
    {
        if (!this.IsCore)
        {
            DisplaySingleton.Instance.DisplayText(x =>
            {
                x.SetText($"FreezeClientModel: {this.name} isn't a core!");
            }, 3f);
        }

        foreach (GeometricChip chip in this.AllChips)
        {
            var r = chip.GetComponent<Rigidbody>();
            if (r != null)
            {
                chip.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    public void SetAfterBuildListeners(Action[] actions)
    {
        this._AfterBuildActions = actions;
    }

    public void HandleInputs()
    {
        //Debug.Assert(this.isLocalPlayer);
        //this.loopScript.HandleInputs();
    }
}
