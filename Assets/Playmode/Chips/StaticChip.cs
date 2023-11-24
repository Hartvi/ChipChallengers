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

    public static Vector3 OrientationToLocalOffset(int orientation)
    {
        switch ((LocalDirection)orientation)
        {
            case LocalDirection.North: return Vector3.forward;
            case LocalDirection.West: return Vector3.left;
            case LocalDirection.South: return Vector3.back;
            case LocalDirection.East: return Vector3.right;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 OrientationToLocalAxisDirectionChip(int orientation)
    {
        switch ((LocalDirection)orientation)
        {
            case LocalDirection.North: return Vector3.left;
            case LocalDirection.West: return Vector3.back;
            case LocalDirection.South: return Vector3.right;
            case LocalDirection.East: return Vector3.forward;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 OrientationToLocalAxisDirectionRudder()
    {
        return Vector3.down;
    }

    public static Vector3 OrientationToLocalAxisDirectionAxle(int orientation)
    {
        switch ((LocalDirection)orientation)
        {
            case LocalDirection.North: return Vector3.back;
            case LocalDirection.West: return Vector3.right;
            case LocalDirection.South: return Vector3.forward;
            case LocalDirection.East: return Vector3.left;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 OrientationToLocalAxisOrigin(int orientation)
    {
        switch ((LocalDirection)orientation)
        {
            case LocalDirection.North: return 0.5f * Vector3.forward;
            case LocalDirection.West: return 0.5f * Vector3.left;
            case LocalDirection.South: return 0.5f * Vector3.back;
            case LocalDirection.East: return 0.5f * Vector3.right;
            default: throw new ArgumentException($"Orientation cannot be outside [0, 3]; you are attempting {orientation}");
        }
    }

    public static Vector3 GetThisGlobalOffsetWrtParent(GeometricChip gc)
    {
        var localOffset = StaticChip.OrientationToLocalOffset(gc.equivalentVirtualChip.orientation);

        Vector3 newGlobalPosition = gc.parentChip.transform.TransformPoint(localOffset);

        return newGlobalPosition;
    }

    public static (Vector3, Vector3) GetThisAxisOfRotationWrtParent(GeometricChip gc, CTP ctp)
    {
        Vector3 localAxisDirection;

        switch (ctp)
        {
            case CTP.Axle:
                localAxisDirection = StaticChip.OrientationToLocalAxisDirectionAxle(gc.equivalentVirtualChip.orientation);
                break;
            case CTP.Rudder:
                localAxisDirection = StaticChip.OrientationToLocalAxisDirectionRudder();
                break;
            default:
                localAxisDirection = StaticChip.OrientationToLocalAxisDirectionChip(gc.equivalentVirtualChip.orientation);
                break;
        }
        var localAxisOrigin = StaticChip.OrientationToLocalAxisOrigin(gc.equivalentVirtualChip.orientation);

        Vector3 globalAxisOrigin = gc.parentChip.transform.TransformPoint(localAxisOrigin);
        Vector3 globalAxisDirection = gc.parentChip.transform.TransformDirection(localAxisDirection);

        return (globalAxisOrigin, globalAxisDirection);
    }

    public static T InstantiateChip<T>(string type) where T : GeometricChip
    {
        if (!VChip.chipTemplates.ContainsKey(type))
        {
            throw new ArgumentException($"Chip of type {type} doesn't exist.");
        }

        CommonChip baseChip = Instantiate(VChip.baseChip);

        GameObject newChip = Instantiate(VChip.chipTemplates[type]);

        newChip.transform.SetParent(baseChip.transform, false);
        newChip.transform.localScale = Vector3.one;

        return baseChip.gameObject.GetComponent<T>();
    }

    public static Vector3 RaycastFromAbove()
    {
        // Starting position of the ray
        Vector3 startPosition = new Vector3(0, 100000, 0);

        // Direction of the ray (vertically down)
        Vector3 direction = Vector3.down;

        // Create a LayerMask to ignore layers 6 and 7
        int layerMask = ~((1 << 6) | (1 << 7));

        // Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(startPosition, direction, out hit, Mathf.Infinity, layerMask))
        {
            // If hit, return the position of the hit point
            return hit.point + Vector3.up*5f;
        }

        // If nothing hit, return (0,0,0)
        return Vector3.zero;
    }
}
