using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StaticChip : MonoBehaviour
{

    // PUBLIC GENERAL CONSTANTS
    public const float ChipSide = 0.5f;
    public static readonly Vector3 ChipSize = new Vector3(ChipSide, 0.02f * ChipSide, ChipSide);
    public const string inverseStr = "inverse";

    public static Vector3 OrientationToLocalOffset(int orientation) { 
        switch(orientation) {
            case 0: return Vector3.forward;
            case 1: return Vector3.left;
            case 2: return Vector3.back;
            case 3: return Vector3.right;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 OrientationToLocalAxisDirection(int orientation) { 
        switch(orientation) {
            case 0: return Vector3.left;
            case 1: return Vector3.back;
            case 2: return Vector3.right;
            case 3: return Vector3.forward;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }
    public static Vector3 OrientationToLocalAxisOrigin(int orientation) {
        switch (orientation) {
            case 0: return 0.5f*Vector3.forward;
            case 1: return 0.5f*Vector3.left;
            case 2: return 0.5f*Vector3.back;
            case 3: return 0.5f*Vector3.right;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 GetThisGlobalOffsetWrtParent(GeometricChip gc) {
        var localOffset = OrientationToLocalOffset(gc.equivalentVirtualChip.orientation);
        Vector3 newGlobalPosition = gc.parentChip.transform.TransformPoint(localOffset);
        return newGlobalPosition;
    }

    public static (Vector3, Vector3) GetThisAxisOfRotationWrtParent(GeometricChip gc) {
        var localAxisDirection = OrientationToLocalAxisDirection(gc.equivalentVirtualChip.orientation);
        var localAxisOrigin = OrientationToLocalAxisOrigin(gc.equivalentVirtualChip.orientation);
        //var localAxisTarget = localAxisOrigin + localAxisDirection;
        Vector3 globalAxisOrigin = gc.parentChip.transform.TransformPoint(localAxisOrigin);
        Vector3 globalAxisDirection = gc.parentChip.transform.TransformDirection(localAxisDirection);
        return (globalAxisOrigin, globalAxisDirection);
    }

    public static T InstantiateChip<T>(string type) where T: GeometricChip {
        if(!VChip.chipTemplates.ContainsKey(type)) {
            throw new ArgumentException($"Chip of type {type} doesn't exist.");
        }
        var newChip = Instantiate(VChip.chipTemplates[type]);

        return newChip.gameObject.GetComponent<T>();
    }

}
