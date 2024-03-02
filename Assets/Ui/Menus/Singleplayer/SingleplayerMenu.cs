using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum CameraMoveMode
{
    Follow, Free
}

public struct CameraFollowSettings
{
    public float distance;  // meters
    public float up;  // meters
    public float predict;  // [0,1)
    public float sensitivity;  // (0,1]
    public float lowPass; // (0,1)

    public CameraFollowSettings(float distance, float up, float predict, float sensitivity, float lowPass)
    {
        this.distance = distance;
        this.up = up;
        this.predict = predict;
        this.sensitivity = sensitivity;
        this.lowPass = lowPass;
    }
}

public class SingleplayerMenu : BaseMenu, InputReceiver
{
    //public static SingleplayerMenu Instance;
    public static VMap myVMap = new VMap();

    Camera mainCamera;
    //Vector3 oldLookAtPos = Vector3.zero;
    CameraFollowSettings cameraFollowSettings;

    private LoadPanel LoadPanel;
    private MapPanel MapPanel;
    private ControlsPanel ControlsPanel;

    private CommonChip core;

    HUD Hud;

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
            new VirtualProp(PropType.Panel, 1f, typeof(MapPanel))
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

        this.cameraFollowSettings = new CameraFollowSettings(distance: 5f, up: 1f, predict: 0.05f, sensitivity: 1f, lowPass: 0f);

        //Action[] deselectedChipCallbacks = new Action[] { () => UIManager.instance.TurnMeOff(this) };
        //this.deselectedCallbacks.SetCallbacks(deselectedChipCallbacks);

        //this.OnEnterMenu();
        this.selectedCallbacks.SetCallbacks(new Action[] { this.OnEnterMenu, () => UIManager.instance.SwitchToMe(this) });
        this.selectedCallbacks.Invoke();

        this.ControlsPanel = this.gameObject.GetComponentInChildren<ControlsPanel>(true);

        this.core.ResetToDefaultLocation();
        // set the camera position, since it might not catch up fast enough
        Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;
    }

    // when going from other menus:
    void OnEnterMenu()
    {
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

        this.MapPanel.SetOnLoadedCallbacks(onLoadedCallbacksTmp);

        //TODO: load model after entering playmode???
        //print($"Current virtual Model: {this.core.VirtualModel}");
        this.Hud.LinkCore(this.core);

        Camera.main.transform.position = (Camera.main.transform.position - core.transform.position).normalized * 5f + core.transform.position;
        Camera.main.transform.LookAt(core.transform.position);
        //Camera.main.transform.position = this.core.transform.position + Vector3.up * 10f;
    }

    //void Update()
    //{

    //}

    //public Vector3 RaycastFromAbove()
    //{
    //    // Starting position of the ray
    //    Vector3 startPosition = new Vector3(0, 100000, 0);

    //    // Direction of the ray (vertically down)
    //    Vector3 direction = Vector3.down;

    //    // Create a LayerMask to ignore layers 6 and 7
    //    int layerMask = ~((1 << 6) | (1 << 7));

    //    // Perform the raycast
    //    RaycastHit hit;
    //    if (Physics.Raycast(startPosition, direction, out hit, Mathf.Infinity, layerMask))
    //    {
    //        // If hit, return the position of the hit point
    //        return hit.point + Vector3.up*5f;
    //    }

    //    // If nothing hit, return (0,0,0)
    //    return Vector3.zero;
    //}


    void CameraFollowMove()
    {
        CameraFollowSettings cs = this.cameraFollowSettings;
        Transform camTransform = this.mainCamera.transform;

        // 20,10,0
        Vector3 corePos = this.core.transform.position;
        // 0
        float predict = cs.predict;
        // 20,10,0 - 18,10,0
        Vector3 lookAtPos = corePos + predict * this.core.rb.velocity;

        //Vector3 deltaCorePos = (corePos - this.oldCorePosition) / Time.deltaTime;
        // 0,0,0
        //Vector3 predictVector = predict * deltaCorePos;
        // 20,10,0 + 0,0,0
        //Vector3 lookAtPos = corePos + predictVector;

        //campos: 16,10,0
        // ((16,10,0 - 20,10,0).mag = 4) - 2 = 2 * 1,0,0 = 2,0,0
        Vector3 deltaPos = Mathf.Max(0f, (camTransform.position - corePos).magnitude - cs.distance) * camTransform.forward;

        float deltaAltitude = (corePos.y + cs.up) - camTransform.position.y;

        // 16,10,0 + 2,0,0
        camTransform.position = camTransform.position + deltaPos + deltaAltitude * Vector3.up;

        camTransform.LookAt(lookAtPos);

        //this.oldLookAtPos = lookAtPos;
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
                //this.MapPanel.gameObject.SetActive(true);
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
                print($"setting active load panel with selected model: {GameManager.Instance.GetModel()}");
                this.LoadPanel.ActivatePanel(GameManager.Instance.GetModel());
                //this.LoadPanel.gameObject.SetActive(true);
            }
#else
            if (Input.GetKeyDown(KeyCode.O))
            {
                this.LoadPanel.ActivatePanel(CommonChip.ClientCore.VirtualModel.ModelName);
                //this.LoadPanel.gameObject.SetActive(true);
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

        switch (GameManager.cameraMoveMode)
        {
            case CameraMoveMode.Follow:
                {
                    this.CameraFollowMove();
                    if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
                    {
                        this.cameraFollowSettings.distance -= 1f;
                    }
                    if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                    {
                        this.cameraFollowSettings.distance += 1f;
                    }
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        this.cameraFollowSettings.up += 1f;
                    }
                    if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        this.cameraFollowSettings.up -= 1f;
                    }
                    break;
                }
            case CameraMoveMode.Free:
                {
                    this.CameraFreeMove();
                    break;
                }
        }

    }

}

