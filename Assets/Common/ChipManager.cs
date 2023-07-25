using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipManager
{
    // PUBLIC GENERAL CONSTANTS
    public const float ChipSize = 0.5f;

    public static Vector3 OrientationToLocalOffset(int orientation) { 
        switch(orientation) {
            case 0: return Vector3.forward;
            case 1: return Vector3.left;
            case 2: return Vector3.back;
            case 3: return Vector3.right;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }
}
