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
    public string modelString = null;
    string clientModelString = null;

    [SyncVar]
    public int serverSideChips = 1;

    public int clientSideChips = 1;
    bool clientTriggerReady => serverSideChips == clientSideChips;

    public InputMessage inputMessage = new InputMessage();

    [SyncVar]
    char[] keysToCheck = new char[0];

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

    private VModel _VirtualModel;
    public VModel VirtualModel
    {
        get
        {
            if (this.isClientOnly)
            {
                if (this.clientModelString != this.modelString)
                {
                    var m = VModel.FromLuaModel(this.modelString);
                    this.equivalentVirtualChip = m.Core;
                    this._VirtualModel = m;
                }
            }
            else if (this._VirtualModel == null)
            {
                throw new NullReferenceException($"Virtual model of core is null.");
            }
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

        TextAsset textFile = Resources.Load<TextAsset>("aguncar");
        this.LoadString(textFile.text);

        Action[] onLoadedCallbacksTmp = new Action[] {
            () => {
                this.TriggerSpawn(false);
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
        foreach (var rtf in this.RuntimeFunctions)
        {
            rtf.RuntimeFunction();
        }
        this.HandleInputs();
    }

    [Command]
    public void CmdLoadString(string state)
    {
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

        this.VirtualModel = model;
        this.TriggerSpawn(true);
        this.VirtualModel.AddModelChangedCallback(x => this.TriggerSpawn(true));
        this.VirtualModel.AddModelChangedCallback(x => this.history.SaveState(this.VirtualModel.ToLuaString()));

        this.keysToCheck = this.GetInputCharactersFromModel(state);

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

    [Command]
    public void CmdUndoHistory()
    {
        this.LoadString(this.history.Undo());
    }

    [Command]
    public void CmdRedoHistory()
    {
        this.LoadString(this.history.Redo());
    }

    [Command]
    public void CmdTriggerSpawn()
    {
        this.TriggerSpawn(false);
    }

    [Command]
    public void CmdResetCore()
    {
        print($"clt=>srv: CmdResetCore");
        // delete after build listeners
        this.RpcResetRotationVelocity();
        Action[] actions = new Action[] { };
        this.SetAfterBuildListeners(actions);
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
    public void TriggerSpawn(bool freeze)
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
        this.scriptInstance = new ScriptInstance(this, this.VirtualModel);

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
        // Core + its children
        this.serverSideChips = this.AllChildren.Length + 1;

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
        if (this.isLocalPlayer)
        {
            this.scriptInstance.LinkSensors(this.VirtualModel);
        }

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
        if (this.isServer)
        {
            this.loopScript.HandleInputs();
        }

        if (this.isLocalPlayer)
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
        //foreach (char c in this.inputMessage.Keys)
        //{
        //    print($"PRESSED KEY: {c}");
        //}
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

}
