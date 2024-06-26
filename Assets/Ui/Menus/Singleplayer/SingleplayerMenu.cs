using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class SingleplayerMenu : BaseMenu, InputReceiver
{
    //public static SingleplayerMenu Instance;
    public static VMap myVMap = new VMap();
    public static VScenario myVScenario = new VScenario();

    Camera mainCamera;
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
    private ScenarioPanel ScenarioPanel;
    private ControlsPanel ControlsPanel;

    private CommonChip core;

    HUD Hud;

    public static List<BaseAspect> RuntimeFunctions = new List<BaseAspect>();

    // HUD, etc
    // km/h m/s variables
    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            new VirtualProp(PropType.Panel, 1f, down,
                new VirtualProp(PropType.Image, 0.04f, down,
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
            new VirtualProp(PropType.Panel, 1f, typeof(MapPanel)),
            new VirtualProp(PropType.Panel, 1f, typeof(ScenarioPanel))
        );
    }

    protected override void Start()
    {
        base.Start();

        //SingleplayerMenu.Instance = this;
        this.Hud = this.GetComponentInChildren<HUD>();

        this.mainCamera = Camera.main;

        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();
        this.MapPanel = this.GetComponentInChildren<MapPanel>();
        this.ScenarioPanel = this.GetComponentInChildren<ScenarioPanel>();

        this.cameraFollowSettings = new CameraFollowSettings(posShift: Vector2.up * 3f + Vector2.right * 10f, predict: 0.05f, sensitivity: 1f, lowPass: 0f);

        //Action[] deselectedChipCallbacks = new Action[] { () => UIManager.instance.TurnMeOff(this) };
        //this.deselectedCallbacks.SetCallbacks(deselectedChipCallbacks);

        //this.OnEnterMenu();
        this.selectedCallbacks.SetCallbacks(new Action[] { this.OnEnterMenu, () => UIManager.instance.SwitchToMe(this) });
        this.selectedCallbacks.Invoke();
        this.deselectedCallbacks.SetCallbacks(new Action[] { this.OnLeaveMenu });

        this.ControlsPanel = this.gameObject.GetComponentInChildren<ControlsPanel>(true);

        this.core.ResetToDefaultLocation();
        // set the camera position, since it might not catch up fast enough
        Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;
    }

    void OnLeaveMenu()
    {
        GameManager.RealTimeSettings.InMenu = true;
        Time.fixedDeltaTime = 0.1f;
    }

    void OnEnterMenu()
    {
        string s = UIStrings.PhysicsRate;
        Time.fixedDeltaTime = 1f / (float)(PlayerPrefs.GetInt(s));
        GameManager.RealTimeSettings.InMenu = false;
        GameManager.Instance.UpdateSettings();

        // ignore wheel and default layer collisions
        Physics.IgnoreLayerCollision(7, 0);
        // ignore wheel to wheel collisions
        Physics.IgnoreLayerCollision(7, 7);
        // ignore player to player collisions
        Physics.IgnoreLayerCollision(6, 6);
        // ignore wheel to player collisions
        Physics.IgnoreLayerCollision(7, 6);

        this.core = CommonChip.ClientCore;


        this.core.transform.rotation = Quaternion.identity;
        this.core.rb.velocity = Vector3.zero;

        // delete after build listeners
        this.core.SetAfterBuildListeners(new Action[] { });
        this.core.TriggerSpawn(this.core.VirtualModel, false);

        // when model is loaded: add callbacks to rebuild the model, add callbacks to link the variables to the HUD
        Action[] onLoadedCallbacksTmp = new Action[] {
            () => {
                this.core.TriggerSpawn(this.core.VirtualModel, false);
                this.core.transform.position += Vector3.up;
                },
            () => this.Hud.LinkCore(this.core),
            () => {
                Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;
            }
        };

        this.LoadPanel.SetOnLoadedCallbacks(onLoadedCallbacksTmp);

        var mapLoadedCallbacks = onLoadedCallbacksTmp.Concat(new Action[] { this.core.ResetToDefaultLocation }).ToArray();
        this.MapPanel.SetOnLoadedCallbacks(mapLoadedCallbacks);

        //TODO: load model after entering playmode???
        this.Hud.LinkCore(this.core);

        Camera.main.transform.position = (Camera.main.transform.position - core.transform.position).normalized * 5f + core.transform.position;
        Camera.main.transform.LookAt(core.transform.position);
    }

    void CameraFollowMove()
    {
        CameraFollowSettings cs = this.cameraFollowSettings;
        Transform camTransform = this.mainCamera.transform;

        Vector3 corePos = this.core.transform.position;
        //float predict = cs.predict;
        //Vector3 coreAcceleration = 0.01f * (this.core.rb.velocity - this.lastCoreVelocity) + 0.99f * this.lastCoreAcceleration;
        //Vector3 error = corePos - this.lastCorePos;
        //Vector3 errorerror = error - this.lastCamError;
        //Vector3 lookAtPos = Time.deltaTime * (10f * error + 10f * errorerror + 1f * this.camErrorSum) + this.lastCorePos;

        Vector3 lookAtPos = corePos;

        // the camera stays behind the object:
        Vector3 deltaPos = Mathf.Max(0f, (camTransform.position - corePos).magnitude - cs.posShift.x) * camTransform.forward;

        // camera stays above the object in world coordinates
        //float deltaAltitude = (corePos.y + cs.posShift.y) - camTransform.position.y;
        Vector3 deltaPosY = 0.1f * (corePos.y + cs.posShift.y - camTransform.position.y) * camTransform.up;
        //camTransform.position = camTransform.position + deltaPos + deltaAltitude * Vector3.up;
        camTransform.position = camTransform.position + deltaPos + deltaPosY;

        camTransform.LookAt(lookAtPos);
        //this.lastCorePos = lookAtPos;
        //this.lastCamError = error;
        //this.camErrorSum += error;

        //this.lastCoreVelocity = this.core.rb.velocity;
        //this.lastCoreAcceleration = coreAcceleration;
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
        CommonChip.UnfreezeClientModel();
    }

    void InputReceiver.OnStopReceiving()
    {
        //print($"Singleplayer menu stopping receiving");
        CommonChip.FreezeClientModel();
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        foreach (var rtf in SingleplayerMenu.RuntimeFunctions)
        {
            if (rtf != null)
            {
                rtf.RuntimeFunction();
            }
        }

        this.core.HandleInputs();

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
                this.MapPanel.ActivatePanel(SingleplayerMenu.myVMap.FileName);
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                this.ScenarioPanel.ActivatePanel(SingleplayerMenu.myVScenario.FileName);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                GoToEditor.Function();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                this.core.rb.velocity = Vector3.zero;
                if (!shiftPressed)
                {
                    Quaternion newRot = Quaternion.Euler(0f, this.core.transform.rotation.eulerAngles.y, 0f);
                    this.core.transform.rotation = newRot;
                    //this.core.transform.rotation = Quaternion.identity;
                }
                this.core.transform.position += Vector3.up * 2f;
                this.core.TriggerSpawn(this.core.VirtualModel, false);
            }
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.T))
            {
                this.core.ResetToDefaultLocation();
                // set the camera position, since it might not catch up fast enough
                Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;

                //Vector3 spawnPosition = this.RaycastFromAbove();
                //this.core.rb.velocity = Vector3.zero;
                //if(!shiftPressed)
                //{
                //    this.core.transform.rotation = Quaternion.identity;
                //}
                //this.core.transform.position = spawnPosition;
                //this.core.TriggerSpawn(this.core.VirtualModel, false);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                //print($"setting active load panel with selected model: {GameManager.Instance.GetModel()}");
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());
                //this.LoadPanel.gameObject.SetActive(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.LoadPanel.ActivatePanel(CommonChip.ClientCore.VirtualModel.ModelName);
                //this.LoadPanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                this.ScenarioPanel.ActivatePanel(SingleplayerMenu.myVScenario.FileName);
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                this.core.ResetToDefaultLocation();
                // set the camera position, since it might not catch up fast enough
                Camera.main.transform.position = this.core.transform.position + Vector3.up*10f;

                //Vector3 spawnPosition = RaycastFromAbove();
                //this.core.rb.velocity = Vector3.zero;
                //if(!shiftPressed)
                //{
                //    this.core.transform.rotation = Quaternion.identity;
                //}
                //this.core.transform.position = spawnPosition;
                //this.core.TriggerSpawn(this.core.VirtualModel, false);
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
                    if (Input.GetMouseButton(1))
                    {
                        Vector3 mouseVelocity = Input.mousePosition - this.lastMousePos;
                        Transform camTransform = this.mainCamera.transform;
                        camTransform.position += -0.033f * (mouseVelocity.x * camTransform.right);
                        this.cameraFollowSettings.posShift.y += -0.033f * mouseVelocity.y; //  +  * camTransform.up
                    }
                    if (Input.mouseScrollDelta.y != 0f)
                    {
                        this.cameraFollowSettings.posShift.x += -0.033f * Input.mouseScrollDelta.y;
                    }
                    this.CameraFollowMove();
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

