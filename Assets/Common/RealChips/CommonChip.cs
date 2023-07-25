using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonChip : MonoBehaviour
{

    public CommonChip parentChip = null;
    private List<CommonChip> childChips = new List<CommonChip>();
    public IReadOnlyList<CommonChip> ChildChips => childChips;
    public VirtualChip equivalentVirtualChip;

    public static CommonChip InstantiateChip(string type) {
        if(!VirtualChip.chipTemplates.ContainsKey(type)) {
            throw new ArgumentException($"Chip of type {type} doesn't exist.");
        }
        var newChip = Instantiate(VirtualChip.chipTemplates[type]);

        return newChip;
    }

    public void SetChild(CommonChip childChip) {
        // we don't want duplicates
        if (childChips.Contains(childChip)) {
            throw new ArgumentException($"Chip {this} already has child {childChip}");
        }
        childChips.Add(childChip);
    }

    public void SetParent(CommonChip parentChip) {
        // null parents not allowed in this function - we can only add children to core and lower, not null
        if (this.parentChip != null) {
            throw new ArgumentException($"Parent of {this} must be null, cannot already have had a parent.");
        }
        this.parentChip = parentChip;

        print($"current parent {parentChip.name} of {name}");
        parentChip.SetChild(this);
    }

    public void AddChild(VirtualChip childChip) {
        var childType = childChip.GetChipType();

        CommonChip newChild = InstantiateChip(childType);

        newChild.equivalentVirtualChip = childChip;
        print("this guy: "+name);
        newChild.SetParent(this);
        var localOffset = ChipManager.OrientationToLocalOffset(childChip.orientation);
        var newChildGlobalPosition = this.transform.TransformPoint(localOffset);
        newChild.transform.position = newChildGlobalPosition;
        newChild.transform.rotation = this.transform.rotation;
    }

    public void AddChildren() {
        foreach (VirtualChip childChip in equivalentVirtualChip.children) {
            AddChild(childChip);
        }
        foreach(var child in ChildChips) {
            child.AddChildren();
        }
    }

    public void TriggerSpawn(VirtualChip core) {
        // do cleanup of old model
        // TODO: CLEANUP

        var coreTypeStr = core.instanceProperties[VirtualChip.typeStr];

        if(coreTypeStr != "Core") {
            throw new ArgumentException($"Cannot trigger spawn from a non-core chip. (Current: {coreTypeStr})");
        }
        equivalentVirtualChip = core;

        AddChildren();  // trigger the tsunami
    }
}
