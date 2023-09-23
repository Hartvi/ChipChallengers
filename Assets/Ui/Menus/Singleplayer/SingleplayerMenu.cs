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

public class SingleplayerMenu : BaseMenu
{
    public static SingleplayerMenu Instance;

    Camera mainCamera;
    Vector3 oldCorePosition = Vector3.zero;
    CameraFollowSettings cameraFollowSettings;

    private LoadPanel LoadPanel;
    
    private CommonChip core;

    CameraMoveMode cameraMoveMode = CameraMoveMode.Follow;

    // HUD, etc
    // km/h m/s variables
    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            new VirtualProp(PropType.Panel, 1f, down, typeof(IntroScreen),
                new VirtualProp(PropType.Panel, 0.2f),
                new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle)),
                new VirtualProp(PropType.Panel, 0.2f, right,
                    new VirtualProp(PropType.Panel, 0.3f),
                    new VirtualProp(PropType.Text, 0.1f),
                    new VirtualProp(PropType.Text, 0.3f)
                    )
            ),
            new VirtualProp(PropType.Panel, 1f, right, typeof(HUD),
                new VirtualProp(PropType.Panel, 0.1f, up,
                    new VirtualProp(PropType.Text, 0.05f, typeof(ItemBase))
                ),
                new VirtualProp(PropType.Panel, 0.1f, up,
                    new VirtualProp(PropType.Text, 0.05f, typeof(ItemBase))
                )
            //new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
            ),
            new VirtualProp(PropType.Panel, 1f, typeof(LoadPanel))
        //new VirtualProp(PropType.Panel, 1f, Vector2Int.down,
        //    new VirtualProp(PropType.Panel, 0.2f),
        //    new VirtualProp(PropType.Text, 0.2f, typeof(MainTitle))
        //new VirtualProp(PropType.Button, 0.2f, typeof(GoToSingleplayer))
        //new VirtualProp(PropType.Button, 0.2f, typeof(GoToMultiplayer)),
        //new VirtualProp(PropType.Button, 0.2f, typeof(GoToEditor)),
        //new VirtualProp(PropType.Button, 0.2f, typeof(GoToOptions)),
        //)
        );
    }

    protected override void Start()
    {
        base.Start();

        SingleplayerMenu.Instance = this;

        this.mainCamera = Camera.main;
        this.LoadPanel = this.GetComponentInChildren<LoadPanel>();

        this.cameraFollowSettings = new CameraFollowSettings(distance: 5f, up: 1f, predict: 0f, sensitivity: 1f, lowPass: 0f);

        this.OnEnterMenu();
        this.selectedCallbacks.SetCallbacks(new Action[] { this.OnEnterMenu });
    }

    // when going from other menus:
    void OnEnterMenu()
    {

        // ignore wheel and default layer collisions
        Physics.IgnoreLayerCollision(7, 0);
        // ignore wheel to wheel collisions
        Physics.IgnoreLayerCollision(7, 7);
        // ignore player to player collisions
        Physics.IgnoreLayerCollision(6, 6);
        // ignore wheel to player collisions
        Physics.IgnoreLayerCollision(7, 6);

        this.core = CommonChip.ClientCore;
        this.core.TriggerSpawn(this.core.VirtualModel, false);

        this.LoadPanel.SetOnLoadedCallbacks(new Action[] { () => this.core.TriggerSpawn(this.core.VirtualModel, false) });
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.Space))
        {
            //core.transform.position += 0.1f*Vector3.up;
            Rigidbody r = core.GetComponent<Rigidbody>();
            r.AddForce(10f*Vector3.up, ForceMode.VelocityChange);
        }
#endif
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrlPressed)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                this.LoadPanel.gameObject.SetActive(true);
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                GoToEditor.Function();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoToMainMenu.Function();
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            this.cameraMoveMode = CameraMoveMode.Follow;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            this.cameraMoveMode = CameraMoveMode.Free;
        }

        switch (this.cameraMoveMode)
        {
            case CameraMoveMode.Follow:
                this.CameraFollowMove();
                break;
            case CameraMoveMode.Free:
                this.CameraFreeMove();
                break;
        }

    }

    void CameraFollowMove()
    {
        CameraFollowSettings cs = this.cameraFollowSettings;
        Transform camTransform = this.mainCamera.transform;

        // 20,10,0
        Vector3 corePos = this.core.transform.position;
        // 0
        float predict = cs.predict;
        // 20,10,0 - 18,10,0
        Vector3 deltaCorePos = corePos - this.oldCorePosition;
        // 0,0,0
        Vector3 predictVector = predict*deltaCorePos;
        // 20,10,0 + 0,0,0
        Vector3 lookAtPos = corePos + predictVector;

        //campos: 16,10,0
        // ((16,10,0 - 20,10,0).mag = 4) - 2 = 2 * 1,0,0 = 2,0,0
        Vector3 deltaPos = Mathf.Max(0f, (camTransform.position - corePos).magnitude - cs.distance) * camTransform.forward;

        float deltaAltitude = (corePos.y + cs.up) - camTransform.position.y;

        // 16,10,0 + 2,0,0
        camTransform.position = camTransform.position + deltaPos + deltaAltitude * Vector3.up;

        camTransform.LookAt(lookAtPos);

        this.oldCorePosition = corePos;
    }

    void CameraFreeMove()
    {
        if (Input.GetMouseButton(1))
        {
            // 
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");
            Camera cam = this.mainCamera;

            cam.transform.Rotate(2f*moveX*Vector3.up, Space.World);
            cam.transform.Rotate(-2f*moveY*Vector3.right, Space.Self);
            
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

            float sensitivity = Input.GetKey(KeyCode.LeftShift) ? 0.09f : 0.03f;
            cam.transform.position = cam.transform.position + sensitivity * deltaPos;
        }
    }
}

