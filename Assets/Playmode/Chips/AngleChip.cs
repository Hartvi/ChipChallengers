using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AngleChip : GeometricChip {

    protected float GetAngle() {
        // TODO: make it variable-compatible
        //float angle = (float)(this.equivalentVirtualChip.instanceProperties[VirtualChip.angleStr]);
        string angleStr;
        //if (!this.equivalentVirtualChip.TryGetProperty<string>(VChip.angleStr, out angleStr)) {
        //    return 0f;
        //}
        angleStr = ArrayExtensions.AccessLikeDict(VChip.angleStr, this.equivalentVirtualChip.keys, this.equivalentVirtualChip.vals);
        
        if (angleStr is null) return 0f;

        print($"anglestr: {angleStr}");
        
        float angle;
        
        if(float.TryParse(angleStr, out angle))
        {
            return angle;
        }

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

