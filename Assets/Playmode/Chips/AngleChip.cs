using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AngleChip : GeometricChip {

    protected float GetAngle() {
        // TODO: make it variable-compatible
        //float angle = (float)(this.equivalentVirtualChip.instanceProperties[VirtualChip.angleStr]);
        string angleStr;
        if (!this.equivalentVirtualChip.TryGetProperty<string>(VChip.angleStr, out angleStr)) {
            return 0f;
        }

        float angle;
        //print($"angle is {angleStr}");
        if (StringHelpers.IsVariableName(angleStr)) {
            // TODO: do variable angles
            throw new NotImplementedException($"Variable angle compatibility has not been implemented yet.");
        } else {
            // exception here says something is wrong
            angle = float.Parse(angleStr);
        }
        
        return angle;
    }
}

