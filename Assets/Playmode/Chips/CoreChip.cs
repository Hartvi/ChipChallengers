using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoreChip : CommonChip
{
    public List<BaseAspect> RuntimeFunctions = new List<BaseAspect>();

    [SyncVar]
    float[] variableValues = new float[0];

    [SyncVar]
    public string modelString = null;
    public string loadedModelString = null;
    [SyncVar]
    public int numberOfChips = -1;
    [SyncVar]
    public uint[] netIds = new uint[0];
    public uint[] oldNetIds = new uint[0];

    [SyncVar]
    public uint srvResetCounter = 0;
    public uint cltResetCounter = 0;

    public InputMessage inputMessage = new InputMessage();

    [SyncVar]
    char[] keysToCheck = new char[0];
    [SyncVar]
    public bool freeze = false;
    bool frozen = false;
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
            return this.AllChildren.Concat(new[] { this }).ToArray();
        }
    }

    private VModel _ServerVirtualModel;
    private VModel _ClientVirtualModel;
    public VModel VirtualModel
    {
        get
        {
            if (this.isServer)
            {
                if (this._ServerVirtualModel == null)
                {
                    throw new NullReferenceException($"{this.netId}: Server virtual model of core is null.");
                }
                return this._ServerVirtualModel;
            }
            if (this._ClientVirtualModel == null)
            {
                throw new NullReferenceException($"{this.netId}: Client virtual model of core is null.");
            }
            return this._ClientVirtualModel;
        }
        set
        {
            // Only the server can load models
            if (this.isServer)
            {
                this.modelString = value.ToLuaString();
                this._ServerVirtualModel = value;
            }
            if (this.isClient)
            {
                this._ClientVirtualModel = value;
            }
            this.equivalentVirtualChip = value.Core;
        }
    }

    public CommonChip[] AllChildren = { };

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

        TextAsset textFile = Resources.Load<TextAsset>("aguncar");
        this.LoadString(textFile.text);

        //Action[] onLoadedCallbacksTmp = new Action[] {
        //    () => {
        //        this.TriggerSpawn(false);
        //        this.transform.position += Vector3.up;
        //        }
        //    //() => this.Hud.LinkCore(this),
        //    //() => {
        //    //    Camera.main.transform.position = this.transform.position + Vector3.up * 10f;
        //    //}
        //};
        //this.OnLoadedCallbacks = onLoadedCallbacksTmp;
    }

    [Client]
    void Update()
    {
        // Wait until we can check that all netids exist and that their length is equal to numberofchips
        if (this.netIds.Length == this.numberOfChips && (this.srvResetCounter != this.cltResetCounter))
        {
            //print($"{this.netId}: netId: {this.netIds.Last()}  len: {this.netIds.Length}");
            bool ready = true;
            foreach (var netId in this.netIds)
            {
                if (!NetworkClient.spawned.ContainsKey(netId))
                {
                    print($"Not spawned: {netId}");
                    ready = false;
                }
            }
            if (ready)
            {
                print($"{this.netId}: triggering spawn: netids: {this.netIds.Length} oldnetids: {this.oldNetIds.Length} cltcounter: {this.cltResetCounter} srvcounter: {this.srvResetCounter} model strings equal: {this.loadedModelString == this.modelString}");
                this.VirtualModel = VModel.FromLuaModel(this.modelString);
                this.VirtualModel.AddModelChangedCallback(
                    x =>
                    {
                        var s = x.ToLuaString();
                        this.CmdLoadString(s);
                    }
                );
                this.TriggerSpawn();
                this.SrvFreezeClientModel(true);
                this.frozen = true;
                this.oldNetIds = this.netIds;
                this.loadedModelString = this.modelString;
                this.cltResetCounter = this.srvResetCounter;
                //if(this == CoreChip.ClientCoreChip)
                //{
                //    SingleplayerMenu.Hud.LinkCore(this.core);
                //}
                print($"{this.netId}: finished spawn");
            }
        }
        else if (this.frozen)
        {
            this.SrvFreezeClientModel(this.freeze);
            this.frozen = false;
        }
        //return;
        foreach (var rtf in this.RuntimeFunctions)
        {
            if (rtf != null)
            {
                rtf.RuntimeFunction();
            }
            else
            {
                //print($"{this.netId}: RTF: {rtf}");
            }
        }
        this.HandleInputs();
    }

    [Command]
    public void CmdLoadString(string state)
    {
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

        this.VirtualModel = model;
        this.variableValues = new float[model.variables.Length];
        this.history.SaveState(this.modelString);

        this.keysToCheck = this.GetInputCharactersFromModel(state);

        StartCoroutine(this.GenerateChips());
        foreach (Action a in this.OnLoadedCallbacks)
        {
            a();
        }
    }

    char[] GetInputCharactersFromModel(string m)
    {
        List<char> ks = new List<char>();
        string parenthesesPattern = @"(\(""([a-zA-Z])""\))|(\('([a-zA-Z])'\))";

        string keyDownPattern = UIStrings.KeyDown + parenthesesPattern;
        string keyUpPattern = UIStrings.KeyUp + parenthesesPattern;
        string keyPattern = UIStrings.Key + parenthesesPattern;

        MatchCollection matches = Regex.Matches(m, keyDownPattern);
        foreach (Match match in matches)
        {
            string capturedCharacter = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
            ks.Add(capturedCharacter[0]);
        }

        matches = Regex.Matches(m, keyUpPattern);
        foreach (Match match in matches)
        {
            string capturedCharacter = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
            ks.Add(capturedCharacter[0]);
        }

        matches = Regex.Matches(m, keyPattern);
        foreach (Match match in matches)
        {
            string capturedCharacter = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
            ks.Add(capturedCharacter[0]);
        }
        return ks.ToArray();
    }

    //[Command]
    //public void CmdUndoHistory()
    //{
    //    this.LoadString(this.history.Undo());
    //}

    //[Command]
    //public void CmdRedoHistory()
    //{
    //    this.LoadString(this.history.Redo());
    //}

    [Command]
    public void CmdTriggerSpawn()
    {
        StartCoroutine(this.GenerateChips());
    }

    [Command]
    public void CmdResetCore()
    {
        print($"clt=>srv: CmdResetCore");
        this.rb.isKinematic = true;
        this.transform.rotation = Quaternion.identity;
        this.srvResetCounter += 1;
    }

    [Command]
    public void CmdResetToDefaultLocation()
    {
        print($"clt=>srv: CmdResetToDefaultLocation");
        this.rb.isKinematic = true;
        Vector3 spawnPosition = StaticChip.RaycastFromAbove();
        this.transform.position = spawnPosition;
        this.transform.rotation = Quaternion.identity;
        this.srvResetCounter += 1;
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

    private IEnumerator WaitForChipsToBeOnline(List<CommonChip> children)
    {
        HashSet<uint> netids = new HashSet<uint>();
        while (netids.Count < children.Count)
        {
            // Check if chips are initialized
            foreach (var chip in children)
            {
                if (chip.isClient && chip.netId != 0)
                {
                    netids.Add(chip.netId);
                }
            }

            yield return null; // Wait until next frame
        }
    }

    [Server]
    public IEnumerator GenerateChips()
    {
        // ONLY ON THE SERVER
        // spawn function:
        // Destroy all of my existing chips
        if (this.AllChildren is not null)
        {
            foreach (var c in this.AllChildren)
            {
                NetworkServer.Destroy(c.gameObject);
            }
        }
        this.netIds = new uint[] { this.netId };
        var allchildren = new List<CommonChip>();
        // First instantiate all chips on the clients
        foreach (var vc in this.VirtualModel.chips)
        {
            if (vc.IsCore)
            {
                this.myCoreNetId = this.netId;
                this.stringId = vc.id;
                // Continue because we don't want to spawn the core
                continue;
            }
            var childType = vc.ChipType;
            CommonChip newChild = GeometricChip.InstantiateChip<CommonChip>(childType);
            newChild.myCoreNetId = this.netId;
            newChild.stringId = vc.id;
            newChild.rb.isKinematic = true;
            allchildren.Add(newChild);
            NetworkServer.Spawn(newChild.gameObject);
        }
        this.AllChildren = allchildren.ToArray();
        this.numberOfChips = this.AllChildren.Length + 1;
        yield return StartCoroutine(WaitForChipsToBeOnline(allchildren));
        print($"GENERATED CHIPS: {this.netId}: netids: {this.netIds.Length}");
        this.srvResetCounter += 1;
        yield break;
    }

    [Client]
    public void TriggerSpawn()
    {
        // Trigger spawn only later on when all the chips are visible on the client
        this.RuntimeFunctions.Clear();

        foreach (VVar v in this.VirtualModel.variables)
        {
            v.valueChangedCallbacks = new Action<float, VVar>[] { };
            v.currentValue = v.defaultValue;
        }

        //this.transform.localScale = StaticChip.ChipSize;

        // this should replace the argument
        VChip core = this.VirtualModel.Core;

        this.equivalentVirtualChip = core;

        if (!this.equivalentVirtualChip.IsCore)
        {
            throw new ArgumentException($"Cannot trigger spawn from a non-core chip. (Current: {core.ChipType})");
        }

        this.SetupRigidbody();

        // handle script
        this.scriptInstance = new ScriptInstance(this, this.VirtualModel);

        // Destroy loop script component
        for (int i = 0; i < 10; ++i)
        {
            if (this.GetComponent<LoopScript>())
            {
                GameObject.Destroy(this.GetComponent<LoopScript>());
            }
        }
        this.loopScript = this.gameObject.AddComponent<LoopScript>();
        this.loopScript.vModel = this.VirtualModel;
        this.loopScript.loopFunction = this.scriptInstance.CallLoop;


        // this performs clean-up as well
        this.myCore = this;
        // AllChildren on the client
        this.AllChildren = this.AddChildren(this);  // trigger the tsunami

        // TODO: remove this and FIX Clipboard
        if (this.VirtualModel.chips.Length != this.AllChips.Length)
        {
            Debug.LogWarning($"Fix clipboard to get rid of this warning");
            // this is to register chips that haven't been added in
            this.VirtualModel.SetChipsWithoutNotify(this.AllChips.Select(x => x.equivalentVirtualChip).ToArray());
        }
        foreach (var rc in this.AllChildren)
        {
            Debug.Assert(rc.equivalentVirtualChip != null);
        }
        this.scriptInstance.LinkSensors(this.VirtualModel);
        this.UncollideModel((CommonChip[])this.AllChips);

        foreach (var a in this._AfterBuildActions)
        {
            a();
        }
    }

    void UncollideModel(CommonChip[] ccs)
    {
        foreach (var chip in ccs)
        {
            foreach (var otherChip in ccs)
            {
                Physics.IgnoreCollision(chip.GetComponent<Collider>(), otherChip.GetComponent<Collider>());
            }
        }
    }

    void UncollideNeighbours(CommonChip[] ccs)
    {
        foreach (var chip in ccs)
        {
            CommonChip parent = (CommonChip)chip.parentChip;
            // Ignore collisions between parent and child
            // Degree of separation = 1
            if (parent != null)
            {
                Physics.IgnoreCollision(parent.GetComponent<Collider>(), chip.GetComponent<Collider>());
            }
            var childChips = ccs.Where(x => x.parentChip == chip);
            foreach (var childChip in childChips)
            {
                // Degree of separation = 2
                if (parent != null)
                {
                    Physics.IgnoreCollision(parent.GetComponent<Collider>(), childChip.GetComponent<Collider>());
                }
                foreach (var otherChildChip in childChips)
                {
                    if (otherChildChip != childChip)
                    {
                        Physics.IgnoreCollision(otherChildChip.GetComponent<Collider>(), childChip.GetComponent<Collider>());
                    }
                }
            }
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
    public void SrvFreezeClientModel(bool f)
    {
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
                r.isKinematic = f;
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

    void SetVariablesSync()
    {
        float[] newVars = new float[this.VirtualModel.variables.Length];
        for (int i = 0; i < this.VirtualModel.variables.Length; ++i)
        {
            newVars[i] = this.VirtualModel.variables[i].currentValue;
        }
        this.variableValues = newVars;
    }

    [Client]
    void TransferControls()
    {
        List<char> ks = new List<char>();
        List<char> kds = new List<char>();
        List<char> kus = new List<char>();
        foreach (var k in this.keysToCheck)
        {
            if (Input.GetKey(InputHelper.chartoKeycode[k]))
            {
                ks.Add(k);
            }
            if (Input.GetKeyDown(InputHelper.chartoKeycode[k]))
            {
                kds.Add(k);
            }
            if (Input.GetKeyUp(InputHelper.chartoKeycode[k]))
            {
                kus.Add(k);
            }
        }
        bool[] bs = new bool[3] { Input.GetMouseButton(0), Input.GetMouseButton(1), Input.GetMouseButton(2) };
        bool[] bds = new bool[3] { Input.GetMouseButtonDown(0), Input.GetMouseButtonDown(1), Input.GetMouseButtonDown(2) };
        bool[] bus = new bool[3] { Input.GetMouseButtonUp(0), Input.GetMouseButtonUp(1), Input.GetMouseButtonUp(2) };
        float[] mouse = new float[2] { Input.mousePosition.x, Input.mousePosition.y };
        this.CmdSendInputsToServer(ks.ToArray(), kds.ToArray(), kus.ToArray(), bs, bds, bus, mouse);
    }

    [Client]
    void SetSyncVariableValues()
    {
        for (int i = 0; i < this.variableValues.Length; ++i)
        {
            this.VirtualModel.variables[i].currentValue = this.variableValues[i];
        }
    }

    public void HandleInputs()
    {
        if (this.isServer)
        {
            this.loopScript?.HandleInputs();
            this.SetVariablesSync();
        }

        if (this.isLocalPlayer)
        {
            this.TransferControls();
            SetSyncVariableValues();
        }
    }

    [Command]
    void CmdSendInputsToServer(char[] ks, char[] kds, char[] kus, bool[] bs, bool[] bds, bool[] bus, float[] mouse)
    {
        this.inputMessage.Keys = ks;
        this.inputMessage.KeysDown = kds;
        this.inputMessage.KeysUp = kus;

        this.inputMessage.MouseClicked = bs;
        this.inputMessage.MouseDown = bds;
        this.inputMessage.MouseUp = bus;
        this.inputMessage.MousePos = mouse;
    }

    public override void OnStopServer()
    {
        if (!this.isServer) { return; }
        foreach (var c in this.AllChildren)
        {
            NetworkServer.Destroy(c.gameObject);
        }
    }
}
