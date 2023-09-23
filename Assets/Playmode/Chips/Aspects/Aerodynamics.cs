using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// const float ConstantPartOfDrag = PhysicsData.CdRectangle * Aerodynamics.ConstantPartOfDragAndLift;
// lift coefficient should be 1 or 1.75 max
//const float ConstantPartOfLift = Aerodynamics.ConstantPartOfDragAndLift;

public class Aerodynamics : BaseAspect
{
    // https://en.wikipedia.org/wiki/Drag_(physics)#Aerodynamics
    const float ConstantPartOfDragAndLift = 1f / 2f * PhysicsData.seaLevelDensity * (GeometricChip.ChipSide * GeometricChip.ChipSide);

    void Start()
    {
        Aerodynamics[] aes = this.gameObject.GetComponents<Aerodynamics>();
        if(aes.Length > 1)
        {
            for(int i = 0; i < aes.Length; ++i)
            {
                if(aes[i] != this) {
                    Debug.LogWarning($"Removing duplicate aerodynamics. FIX THIS");
                    GameObject.Destroy(aes[i]);
                }
            }
        }
    }

    void FixedUpdate()
    {
        Transform t = this.transform;
        Vector3 velocity = this.rb.velocity;

        // drag
        Vector3 up = t.up;
        float DragProportion = Vector3.Dot(up, velocity);
        this.rb.AddForce(-Mathf.Sign(DragProportion)*up*ConstantPartOfDragAndLift*DragProportion*DragProportion);
    }
}
