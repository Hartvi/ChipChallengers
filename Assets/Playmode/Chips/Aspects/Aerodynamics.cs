using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// const float ConstantPartOfDrag = PhysicsData.CdRectangle * Aerodynamics.ConstantPartOfDragAndLift;
// lift coefficient should be 1 or 1.75 max
//const float ConstantPartOfLift = Aerodynamics.ConstantPartOfDragAndLift;

public class Aerodynamics : BaseAspect
{
    // https://en.wikipedia.org/wiki/Drag_(physics)#Aerodynamics
    const float ConstantPartOfDragAndLift = 1f * PhysicsData.seaLevelDensity * (GeometricChip.ChipSide * GeometricChip.ChipSide);

    void Start()
    {
        Aerodynamics[] aes = this.gameObject.GetComponents<Aerodynamics>();
        if (aes.Length > 1)
        {
            for (int i = 0; i < aes.Length; ++i)
            {
                if (aes[i] != this)
                {
                    Debug.LogWarning($"Removing duplicate aerodynamics. FIX THIS");
                    GameObject.Destroy(aes[i]);
                }
            }
        }
    }

    void Update()
    {
        Transform t = this.transform;
        Vector3 velocity = this.rb.velocity;
        int underwater = this.transform.position.y < 0f ? 1 : 0;
        //if (underwater == 1)
        //{
        //    velocity *= Mathf.Max(80f / (1f + velocity.magnitude), 2f);
        //}

        // drag
        Vector3 up = t.up;
        float DragAndLiftProportion = Vector3.Dot(up, velocity);
        // extra drag is needed because lift is roughly proportional to 1 whereas drag is roughly proportional to 2
        // direction of extra drag

        //Vector3 ExtraDrag = Vector3.Project(up, velocity) * velocity.sqrMagnitude;

        float commonConstant = -Mathf.Sign(DragAndLiftProportion) * ConstantPartOfDragAndLift;

        // nothing is an airfoil:
        //this.rb.AddForce(commonConstant * (up * DragAndLiftProportion * DragAndLiftProportion + ExtraDrag));

        // everything is an aerofoil with infinite stall speed:
        float invDeltaTime = 1f / Time.deltaTime;
        this.rb.AddForce(invDeltaTime * (commonConstant * (up * DragAndLiftProportion * DragAndLiftProportion) - 0.05f * velocity));
        if (underwater == 1)
        {
            // experimentally verified that this does not crash
            this.rb.AddForce(-invDeltaTime * (1f * velocity + 25f * up * DragAndLiftProportion + Vector3.up));
        }
    }

    //void FixedUpdate()
    //{
    //    Transform t = this.transform;
    //    Vector3 velocity = this.rb.velocity;

    //    // drag
    //    Vector3 up = t.up;
    //    Vector3 DragDirection = Vector3.Project(up, velocity);
    //    Vector3 LiftDirection = up - DragDirection;
    //    // how to find the +- multiplication constant?
    //    float commonConstant = -Mathf.Sign(DragAndLiftProportion) * ConstantPartOfDragAndLift;
    //    this.rb.AddForce(commonConstant*(up*DragAndLiftProportion*DragAndLiftProportion + DragDirection));
    //}
}
