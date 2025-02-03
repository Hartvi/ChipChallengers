using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// const float ConstantPartOfDrag = PhysicsData.CdRectangle * Aerodynamics.ConstantPartOfDragAndLift;
// lift coefficient should be 1 or 1.75 max
//const float ConstantPartOfLift = Aerodynamics.ConstantPartOfDragAndLift;

public class Aerodynamics : BaseAspect
{
    // https://en.wikipedia.org/wiki/Drag_(physics)#Aerodynamics
    const float ConstantPartOfDragAndLift = 2f * Mathf.PI * PhysicsData.seaLevelDensity * (GeometricChip.ChipSide * GeometricChip.ChipSide);

    protected override void Start()
    {
        base.Start();
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

    public override void RuntimeFunction()
    {
        Transform t = this.transform;
        Vector3 velocity = this.rb.velocity;
        int underwater = this.transform.position.y < 0f ? 1 : 0;

        Vector3 up = t.up;
        float vDotN = Vector3.Dot(up, velocity);

        float commonConstant = -Mathf.Sign(vDotN) * ConstantPartOfDragAndLift;

        // C * UP VECTOR * VELOCITY * VELOCITY - some drag
        Vector3 finalForce = (commonConstant * (up * vDotN * vDotN) - 0.05f * velocity);
        if (underwater == 1)
        {
            finalForce += -(PhysicsData.seaLevelDensity * 6.28f * velocity + 25f * up * vDotN + Vector3.up);
        }
        this.rb.AddForce(Time.deltaTime * finalForce, ForceMode.Impulse);
    }
}
