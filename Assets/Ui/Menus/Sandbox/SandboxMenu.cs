using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Mirror;

public class SandboxMenu : BaseMenu, InputReceiver
{
    //public static SandboxMenu Instance;
    public static VMap myVMap = new VMap();
    public static VChallenge myVChallenge = new VChallenge();
    bool reloadChallengeAfterBuild;
    int playEntry;
    Coroutine challengeLoading;

    // PlayChallenge remembers the selected challenge and enters play mode without clearing its context.
    public static void PlayChallenge(string name)
    {
        GameManager.PlayContext.SelectChallenge(name);
        GoToSandbox.Function();
    }

    // RestartChallenge unloads all challenge state and queues a fresh Lua load after resetting the player model.
    void RestartChallenge(string name)
    {
        if (!GameManager.PlayContext.IsChallenge || GameManager.PlayContext.ChallengeName != name) { return; }
        this.playEntry++;
        if (this.challengeLoading != null) { StopCoroutine(this.challengeLoading); }
        this.challengeLoading = null;
        myVChallenge.UnloadChallenge();
        this.reloadChallengeAfterBuild = true;
        this.core.CmdResetCore();
    }

    // LeaveChallenge cancels queued challenge loads and removes challenge runtime and context after a manual map load.
    void LeaveChallenge()
    {
        this.playEntry++;
        this.reloadChallengeAfterBuild = false;
        if (this.challengeLoading != null) { StopCoroutine(this.challengeLoading); }
        this.challengeLoading = null;
        GameManager.PlayContext.SelectSandbox();
        myVChallenge.UnloadChallenge();
    }

    // LoadPendingChallenge schedules one fresh challenge load after the player model is built for this play entry.
    void LoadPendingChallenge()
    {
        if (!this.reloadChallengeAfterBuild) { return; }
        this.reloadChallengeAfterBuild = false;
        var context = GameManager.PlayContext;
        if (!context.IsChallenge) { return; }
        this.challengeLoading = StartCoroutine(this.LoadChallengeAfterBuild(context.ChallengeName, context.Revision, this.playEntry));
    }

    // CanLoadChallenge rejects queued loads after leaving play mode or changing the selected challenge.
    bool CanLoadChallenge(string name, int revision, int entry) => this.isActiveAndEnabled &&
        entry == this.playEntry && GameManager.PlayContext.MatchesChallenge(name, revision);

    // LoadChallengeAfterBuild waits for reset bookkeeping and model readiness before restarting the selected challenge.
    IEnumerator LoadChallengeAfterBuild(string name, int revision, int entry)
    {
        yield return null;
        while (this.CanLoadChallenge(name, revision, entry) && NetworkClient.localPlayer != null &&
               (CoreChip.ClientCoreChip.HasPendingPoseReset || CoreChip.ClientCoreChip.IsRebuilding ||
                CoreChip.ClientCoreChip.srvResetCounter != CoreChip.ClientCoreChip.cltResetCounter ||
                CoreChip.ClientCoreChip.loadedModelString != CoreChip.ClientCoreChip.modelString))
        {
            yield return null;
        }
        this.challengeLoading = null;
        if (!this.CanLoadChallenge(name, revision, entry) || NetworkClient.localPlayer == null) { yield break; }
        myVChallenge.LoadNewChallenge(name + UIStrings.ChallengeExtension);
    }

    NetworkManager manager;

    Camera mainCamera;
    const float MaxFollowElevation = 80f;
    const float WakeFollowSpeed = 1f;
    const float WakeHorizontalHalfAngle = 30f;
    const float WakeVerticalHalfAngle = 60f;
    [SerializeField, Min(0.1f)] float followPositionSharpness = 6f;
    [SerializeField, Min(0f)] float horizontalFollowDrag = 0.15f;
    bool userOrbitOutsideWake;
    //Vector3 oldLookAtPos = Vector3.zero;
    CameraFollowSettings cameraFollowSettings;
    Vector3 lastMousePos = Vector3.zero;
    Vector3 lastCorePos = Vector3.zero;
    Vector3 lastCoreVelocity = Vector3.zero;
    Vector3 lastCoreAcceleration = Vector3.zero;
    Vector3 lastCamError = Vector3.zero;
    Vector3 camErrorSum = Vector3.zero;

    private LoadPanel LoadPanel;
    private MapPanel MapPanel;
    private ControlsPanel ControlsPanel;

    private CoreChip core;

    HUD Hud;

    //public static List<BaseAspect> RuntimeFunctions = new List<BaseAspect>();

    // HUD, etc
    // km/h m/s variables
    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            new VirtualProp(PropType.Panel, 1f, down,
                new VirtualProp(PropType.Panel, 0.04f, down,
                    new VirtualProp(PropType.Text, 1f, down, typeof(MiniControls))
                )
            ),
            new VirtualProp(PropType.Panel, 1f, down, typeof(IntroScreen),
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Panel, 0.2f, down,
                    new VirtualProp(PropType.Text, 1f, typeof(MainTitle))
                ),
                new VirtualProp(PropType.Panel, 0.2f, right,
                    new VirtualProp(PropType.Panel, 0.3f),
                    new VirtualProp(PropType.Text, 0.1f),
                    new VirtualProp(PropType.Text, -1f)
                    )
            ),
            new VirtualProp(PropType.Panel, 1f, right, typeof(HUD)),
            new VirtualProp(PropType.Panel, 1f, right, typeof(ControlsPanel)),
            //    new VirtualProp(PropType.Panel, 0.1f, up,
            //        new VirtualProp(PropType.Text, 0.05f, typeof(ItemBaseLeft))
            //    ),
            //    new VirtualProp(PropType.Panel, 0.1f, up,
            //        new VirtualProp(PropType.Text, 0.05f, typeof(ItemBaseRight))
            //    )
            //),
            new VirtualProp(PropType.Panel, 1f, typeof(LoadPanel)),
            new VirtualProp(PropType.Panel, 1f, typeof(MapPanel))
        );
    }

    protected override void Start()
    {
        base.Start();
        this.manager = GameObject.FindObjectOfType<NetworkManager>();

        this.Hud = this.GetComponentInChildren<HUD>();

        this.mainCamera = Camera.main;

        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();
        this.MapPanel = this.GetComponentInChildren<MapPanel>();

        this.cameraFollowSettings = new CameraFollowSettings(posShift: Vector2.up * 3f + Vector2.right * 10f, predict: 0.05f, sensitivity: 1f, lowPass: 0f);

        this.selectedCallbacks.SetCallbacks(new Action[] { () => UIManager.instance.SwitchToMe(this), this.StartOnEnterMenu });
        this.selectedCallbacks.Invoke();
        this.deselectedCallbacks.SetCallbacks(new Action[] { this.OnLeaveMenu });

        this.ControlsPanel = this.gameObject.GetComponentInChildren<ControlsPanel>(true);
    }

    void OnLeaveMenu()
    {
        if (this.core != null) { this.core.ResetCompleted -= this.PositionCameraAfterReset; }
        this.playEntry++;
        this.reloadChallengeAfterBuild = false;
        if (this.challengeLoading != null) { StopCoroutine(this.challengeLoading); }
        this.challengeLoading = null;
        myVChallenge.UnloadChallenge();
        GameManager.RealTimeSettings.InMenu = true;
    }

    void StartOnEnterMenu()
    {
        Debug.Assert(this.isActiveAndEnabled);
        StartCoroutine(AsyncEnterMenu());
    }

    private IEnumerator AsyncEnterMenu()
    {
        bool startingHost = !manager.isNetworkActive && GameManager.isHost;
        if (!manager.isNetworkActive)
        {
            if (GameManager.isHost) { manager.StartHost(); }
            else { manager.StartClient(); }
        }
        while (NetworkClient.localPlayer == null)
        {
            yield return null;
        }
        while (CoreChip.ClientCoreChip.IsRebuilding ||
               CoreChip.ClientCoreChip.loadedModelString == null)
        {
            yield return null;
        }
        if (startingHost)
        {
            var core = CoreChip.ClientCoreChip;
            core.CmdResetToDefaultLocation();
            while (core.HasPendingPoseReset || core.IsRebuilding ||
                   core.srvResetCounter != core.cltResetCounter)
            {
                yield return null;
            }
        }
        this.OnEnterMenu();
    }

    void OnEnterMenu()
    {
        this.playEntry++;
        this.reloadChallengeAfterBuild = GameManager.PlayContext.IsChallenge;
        GameManager.RealTimeSettings.InMenu = false;
        GameManager.Instance.UpdateSettings();

        // ignore wheel and default layer collisions
        Physics.IgnoreLayerCollision(7, 0);
        // ignore wheel to wheel collisions
        Physics.IgnoreLayerCollision(7, 7);
        // ignore player to player collisions
        //Physics.IgnoreLayerCollision(6, 6);
        // ignore wheel to player collisions
        Physics.IgnoreLayerCollision(7, 6);

        if (this.core != null) { this.core.ResetCompleted -= this.PositionCameraAfterReset; }
        this.core = CoreChip.ClientCoreChip;
        this.lastCorePos = this.core.transform.position;
        this.core.ResetCompleted += this.PositionCameraAfterReset;
        GameManager.cameraMoveMode = CameraMoveMode.Follow;
        //print("SET NEW CORE");
        this.core.freeze = false;

        // SEND COMMANDS PERTAINING TO CORE
        this.core.CmdResetCore();

        // when model is loaded: add callbacks to rebuild the model, add callbacks to link the variables to the HUD
        Action[] onLoadedCallbacksTmp = new Action[] {
            //() => {
            //    this.core.TriggerSpawn(this.core.VirtualModel, false);
            //    this.core.transform.position += Vector3.up;
            //    },
            //() => this.Hud.LinkCore(this.core),
            //() => {
            //    //Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;
            //}
        };

        this.LoadPanel.SetOnLoadedCallbacks(onLoadedCallbacksTmp);
        Action[] afterBuildListeners = new Action[] {
            () =>
            {
                this.Hud.LinkCore(this.core);
            },
            this.LoadPendingChallenge
        };
        this.core.SetAfterBuildListeners(afterBuildListeners);

        var mapLoadedCallbacks = onLoadedCallbacksTmp.Concat(new Action[] { this.LeaveChallenge, this.core.CmdResetToDefaultLocation }).ToArray();
        this.MapPanel.SetOnLoadedCallbacks(mapLoadedCallbacks);

        //TODO: load model after entering playmode???
        //Debug.Log("TODO: add HUD");
        //this.wud.LinkCore(this.core);

    }

    // PositionCameraAfterReset places the play camera at the final spawn after model rebuilding and challenge initialization.
    void PositionCameraAfterReset()
    {
        if (!this.isActiveAndEnabled || this.core == null || this.reloadChallengeAfterBuild || this.challengeLoading != null) { return; }
        CoreCameraPlacement.Place(this.mainCamera, this.core.transform,
            Mathf.Max(3f, this.cameraFollowSettings.posShift.x), Mathf.Max(2f, this.cameraFollowSettings.posShift.y));
        this.lastCorePos = this.core.transform.position;
        this.userOrbitOutsideWake = false;
    }

    void CameraFollowMove(bool mouseOrbitMoved)
    {
        this.cameraFollowSettings.posShift.x = Mathf.Max(0.5f, this.cameraFollowSettings.posShift.x);
        this.cameraFollowSettings.posShift.y = Mathf.Min(this.cameraFollowSettings.posShift.y,
            this.cameraFollowSettings.posShift.x * Mathf.Tan(MaxFollowElevation * Mathf.Deg2Rad));
        CameraFollowSettings cs = this.cameraFollowSettings;
        Transform camTransform = this.mainCamera.transform;

        if (this.core == null) { return; }
        Vector3 corePos = this.core.transform.position;
        Vector3 lookAtPos = corePos;

        Vector3 coreMovement = corePos - this.lastCorePos;
        Vector3 followPosition = camTransform.position + coreMovement;
        // Carry the orbit with the core both inside and outside the trailing cone.
        Vector3 targetPosition = followPosition;
        Vector3 orbitOffset = camTransform.position - this.lastCorePos;
        Vector3 coreVelocity = this.core.rb.velocity;
        bool moving = coreVelocity.sqrMagnitude > WakeFollowSpeed * WakeFollowSpeed;
        Vector3 trailingDirection = -coreVelocity;
        float orbitYaw = Mathf.Atan2(orbitOffset.x, orbitOffset.z) * Mathf.Rad2Deg;
        float trailingYaw = Vector3.ProjectOnPlane(trailingDirection, Vector3.up).sqrMagnitude > 0.000001f
            ? Mathf.Atan2(trailingDirection.x, trailingDirection.z) * Mathf.Rad2Deg : orbitYaw;
        float orbitElevation = Mathf.Atan2(orbitOffset.y,
            Vector3.ProjectOnPlane(orbitOffset, Vector3.up).magnitude) * Mathf.Rad2Deg;
        float trailingElevation = Mathf.Atan2(trailingDirection.y,
            Vector3.ProjectOnPlane(trailingDirection, Vector3.up).magnitude) * Mathf.Rad2Deg;
        bool inWake = moving &&
            Mathf.Abs(Mathf.DeltaAngle(trailingYaw, orbitYaw)) <= WakeHorizontalHalfAngle &&
            Mathf.Abs(orbitElevation - trailingElevation) <= WakeVerticalHalfAngle;
        if (mouseOrbitMoved) { this.userOrbitOutsideWake = true; }
        else if (inWake) { this.userOrbitOutsideWake = false; }
        // Height is independent of the horizontal orbit radius and follows world up.
        targetPosition.y = corePos.y + cs.posShift.y;

        if (moving && !this.userOrbitOutsideWake)
        {
            // Clamp horizontal and vertical trailing angles independently, then ease toward them.
            Vector3 wakeOffset = targetPosition - corePos;
            float wakeYaw = Mathf.Atan2(wakeOffset.x, wakeOffset.z) * Mathf.Rad2Deg;
            float wakeElevation = Mathf.Atan2(wakeOffset.y,
                Vector3.ProjectOnPlane(wakeOffset, Vector3.up).magnitude) * Mathf.Rad2Deg;
            wakeYaw = (trailingYaw + Mathf.Clamp(Mathf.DeltaAngle(trailingYaw, wakeYaw),
                -WakeHorizontalHalfAngle, WakeHorizontalHalfAngle)) * Mathf.Deg2Rad;
            wakeElevation = Mathf.Clamp(Mathf.Clamp(wakeElevation, trailingElevation - WakeVerticalHalfAngle,
                trailingElevation + WakeVerticalHalfAngle), -MaxFollowElevation, MaxFollowElevation) * Mathf.Deg2Rad;
            targetPosition = corePos + new Vector3(cs.posShift.x * Mathf.Sin(wakeYaw),
                cs.posShift.x * Mathf.Tan(wakeElevation), cs.posShift.x * Mathf.Cos(wakeYaw));
        }

        // Smooth around a cylinder: yaw, X/Z radius, and height are independent.
        float blend = 1f - Mathf.Exp(-this.followPositionSharpness * Time.deltaTime);
        Vector3 currentOffset = followPosition - corePos;
        Vector3 targetOffset = targetPosition - corePos;
        float currentYaw = Mathf.Atan2(currentOffset.x, currentOffset.z) * Mathf.Rad2Deg;
        // Horizontal drag pulls the orbit behind movement only inside the cone.
        // Apply it only to yaw so vertical following and the orbit radius stay independent.
        float drag = inWake ? this.horizontalFollowDrag : 0f;
        float targetYaw = Mathf.Atan2(targetOffset.x - coreVelocity.x * drag,
            targetOffset.z - coreVelocity.z * drag) * Mathf.Rad2Deg;
        float yaw = Mathf.LerpAngle(currentYaw, targetYaw, blend) * Mathf.Deg2Rad;
        // Only zoom changes the radius; direction changes keep the camera on the orbit.
        float currentRadius = Mathf.Sqrt(currentOffset.x * currentOffset.x + currentOffset.z * currentOffset.z);
        float horizontalRadius = Mathf.Lerp(currentRadius, cs.posShift.x, blend);
        float maxHeight = horizontalRadius * Mathf.Tan(MaxFollowElevation * Mathf.Deg2Rad);
        float height = Mathf.Clamp(Mathf.Lerp(currentOffset.y, targetOffset.y, blend), -maxHeight, maxHeight);
        camTransform.position = corePos + new Vector3(horizontalRadius * Mathf.Sin(yaw),
            height, horizontalRadius * Mathf.Cos(yaw));

        camTransform.LookAt(lookAtPos);
    }

    void CameraCopyInside()
    {
        Transform camTransform = this.mainCamera.transform;
        Transform ct = this.core.transform;

        camTransform.position = ct.position;
        camTransform.rotation = ct.rotation;
    }

    void CameraCopyOutside()
    {
        CameraFollowSettings cs = this.cameraFollowSettings;
        Transform camTransform = this.mainCamera.transform;
        Transform ct = this.core.transform;

        camTransform.position = ct.position - ct.forward * cs.posShift.x + ct.up * cs.posShift.y;
        camTransform.rotation = ct.rotation;
    }

    void CameraFreeMove()
    {
        if (Input.GetMouseButton(1))
        {
            // 
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");
            Camera cam = this.mainCamera;

            cam.transform.Rotate(2f * moveX * Vector3.up, Space.World);
            cam.transform.Rotate(-2f * moveY * Vector3.right, Space.Self);

            Vector3 deltaPos = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                deltaPos = cam.transform.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                deltaPos = -cam.transform.right;
            }
            if (Input.GetKey(KeyCode.S))
            {
                deltaPos = -cam.transform.forward;
            }
            if (Input.GetKey(KeyCode.D))
            {
                deltaPos = cam.transform.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                deltaPos = -cam.transform.up;
            }
            if (Input.GetKey(KeyCode.E))
            {
                deltaPos = cam.transform.up;
            }

            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? Time.deltaTime * 10f : Time.deltaTime * 3f;
            cam.transform.position = cam.transform.position + sensitivity * deltaPos;
        }
    }

    void InputReceiver.OnStartReceiving()
    {
        if (this.core != null) { this.lastCorePos = this.core.transform.position; }
    }

    void InputReceiver.OnStopReceiving()
    {
        if (NetworkClient.localPlayer == null) return;
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        //#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.Space))
        {
            //core.transform.position += 0.1f*Vector3.up;
            Rigidbody r = core.GetComponent<Rigidbody>();
            r.AddForce(10f * Vector3.up, ForceMode.VelocityChange);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            //core.transform.position += 0.1f*Vector3.up;
            Rigidbody r = core.GetComponent<Rigidbody>();
            r.AddForce(1f * Vector3.forward, ForceMode.VelocityChange);
        }
        //#endif
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftPressed = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);

        if (ctrlPressed)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                this.MapPanel.ActivatePanel(SandboxMenu.myVMap.FileName);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                GoToEditor.Function();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (this.core != null)
                {
                    GameManager.PlayContext.Retry(this.RestartChallenge,
                        () => this.core.CmdResetPose(this.core.rb.position + Vector3.up, Quaternion.identity));
                }
            }
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.T))
            {
                this.core.CmdResetToDefaultLocation();
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());

            }
#else
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                this.core.CmdResetToDefaultLocation();
            }
#endif
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoToMainMenu.Function(true);
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            UIManager.instance.SwitchToMe(this.ControlsPanel);
            //this.ControlsPanel.gameObject.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            GameManager.cameraMoveMode = CameraMoveMode.Follow;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            GameManager.cameraMoveMode = CameraMoveMode.Free;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            GameManager.cameraMoveMode = CameraMoveMode.CopyOutside;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            GameManager.cameraMoveMode = CameraMoveMode.CopyInside;
        }

        if (GameManager.cameraMoveMode == CameraMoveMode.Follow || GameManager.cameraMoveMode == CameraMoveMode.CopyOutside)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                this.cameraFollowSettings.posShift += Vector2.up;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                this.cameraFollowSettings.posShift -= Vector2.up;
            }
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                this.cameraFollowSettings.posShift.x -= 1f;
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                this.cameraFollowSettings.posShift.x += 1f;
            }
        }

        switch (GameManager.cameraMoveMode)
        {
            case CameraMoveMode.Follow:
                {
                    bool mouseOrbitMoved = false;
                    if (Input.GetMouseButton(1))
                    {
                        Vector3 mouseVelocity = Input.mousePosition - this.lastMousePos;
                        mouseOrbitMoved = mouseVelocity.x != 0f || mouseVelocity.y != 0f;
                        Transform camTransform = this.mainCamera.transform;
                        camTransform.position += -0.033f * (mouseVelocity.x * camTransform.right);
                        this.cameraFollowSettings.posShift.y += -0.033f * mouseVelocity.y; //  +  * camTransform.up
                    }
                    if (Input.mouseScrollDelta.y != 0f)
                    {
                        this.cameraFollowSettings.posShift.x += -0.165f * Input.mouseScrollDelta.y;
                    }
                    this.CameraFollowMove(mouseOrbitMoved);
                    //if (Input.GetKeyDown(KeyCode.KeypadMultiply) || Input.GetKeyDown(KeyCode.Asterisk))
                    //{
                    //    this.cameraFollowSettings.predict *= 2f;
                    //}
                    //if (Input.GetKeyDown(KeyCode.KeypadDivide) || Input.GetKeyDown(KeyCode.Slash))
                    //{
                    //    this.cameraFollowSettings.predict *= 0.5f;
                    //}
                    this.lastMousePos = Input.mousePosition;
                    break;
                }
            case CameraMoveMode.Free:
                {
                    this.CameraFreeMove();
                    break;
                }
            // Copy position with offset and rotation
            case CameraMoveMode.CopyOutside:
                {
                    this.CameraCopyOutside();
                    break;
                }
            // Copy position & rotation without any offsets etc
            case CameraMoveMode.CopyInside:
                {
                    this.CameraCopyInside();
                    break;
                }
        }

        if (this.core != null) { this.lastCorePos = this.core.transform.position; }

    }

}

public enum CameraMoveMode
{
    Follow, Free, CopyOutside, CopyInside
}

public struct CameraFollowSettings
{
    //public float distance;  // meters
    public Vector2 posShift;  // meters
    public float predict;  // [0,1)
    public float sensitivity;  // (0,1]
    public float lowPass; // (0,1)

    public CameraFollowSettings(Vector2 posShift, float predict, float sensitivity, float lowPass)
    {
        //this.distance = distance;
        this.posShift = posShift;
        this.predict = predict;
        this.sensitivity = sensitivity;
        this.lowPass = lowPass;
    }
}

