using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PRINT;

public static class Clipboard
{
    static VChip newParent = null;

    public static void Copy(string id)
    {
        // public VChip(string[] keys, string[] vals, int orientation, VChip parentChip)
        // public VChip(string chipType, LocalDirection localDirection, VChip parent)

        //VChip virtualCore = new VChip(
        //    new string[]{ VChip.typeStr, VChip.colourStr, VChip.nameStr }, 
        //    new string[] { VChip.coreStr, "#FFFFFF", "VirtualCore" }, 
        //    0, 
        //    null
        //);

        CommonChip core = CommonChip.ClientCore;
        CommonChip cc = core.AllChildren.FirstOrDefault(x => x.equivalentVirtualChip.id == id) as CommonChip;

        if(cc is null)
        {
            return;
        }

        //List<VChip> chips = new List<VChip>();
        var tuple = cc.equivalentVirtualChip.GetValueTuple();

        VChip virtualCore = new VChip(
            tuple.keys, 
            tuple.values,
            tuple.orientation, 
            null
        );
        // begin func
        Clipboard.newParent = virtualCore;
        VChip ChipToBeCopied = cc.equivalentVirtualChip;
        var oldChildren = new VChip[] { ChipToBeCopied };

        VChip[] vcs = CopyChildren(Clipboard.newParent, oldChildren, false);

        //IPrint(GetChipAndChildren(newParent).Select(x => x.id));

        // TODO save sub-model string as new virtual model with an extra core attached as the virtual root
        // then graft it onto the original model and reconfigure parents etc
    }

    public static VChip AttachTo(VChip vc, int newOrientation, bool mirror)
    {
        if (Clipboard.newParent is null) return CommonChip.ClientCore.equivalentVirtualChip;
        // chip we wanna attach it to
        //IPrint($"new parent chip: {vc.ChipType} id: {vc.id}");
        //IPrint($"Orientation: {newOrientation}");

        
        // Clipboard.newParent is the virtual core that we don't want to copy
        // we want to connect Clipboard.Children to the argument `vc`
        // correct the orientation then build
        VChip oldOriginalChild = Clipboard.newParent.Children[0];
        oldOriginalChild.orientation = newOrientation;

        VChip[] oldChildren = new VChip[] { oldOriginalChild };

        VChip[] newChips = Clipboard.CopyChildren(vc, oldChildren, mirror);

        CommonChip core = CommonChip.ClientCore;

        VModel vm = core.VirtualModel;
        vm.chips = vm.chips.Concat(newChips).ToArray();
        // refresh virtual chips
        //vm.chips = vm.GetAllVChips();

        //core.TriggerSpawn(vm, true);

        return oldOriginalChild;
        // TODO make this more secure:
        //this.editorMenu.selectedChip = this.editorMenu.highlighter.SelectVChip(newChip.rChip.equivalentVirtualChip.id);
    }

    private static VChip[] CopyChildren(VChip newParent, VChip[] oldChildren, bool mirror)
    {
        List<VChip> vclist = new List<VChip>();

        void _CopyChildren(VChip newParentss, VChip[] oldChildrenss)
        {
            // func(newParentss, oldChildrenss)
            for (int i = 0; i < oldChildrenss.Length; ++i)
            {
                VChip oldChild = oldChildrenss[i];
                var valueTuple = oldChild.GetValueTuple();
                bool orientationIsLeftOrRight = valueTuple.orientation == 1 || valueTuple.orientation == 3;
                
                VChip newChild = new VChip(
                    valueTuple.keys,
                    valueTuple.values,
                    mirror && orientationIsLeftOrRight ? (valueTuple.orientation + 2) % 4 : valueTuple.orientation,
                    newParentss
                );
                vclist.Add(newChild);
                //IPrint($"pasting new child: {newChild.ChipType}, {newChild.orientation}");
                _CopyChildren(newChild, oldChild.Children);
            }
        }
        _CopyChildren(newParent, oldChildren);
        return vclist.ToArray();
    }

    private static VChip[] GetChipAndChildren(VChip vc)
    {
        List<VChip> _GetChipAndChildren(VChip vc)
        {
            List<VChip> vcs = new List<VChip>();

            for (int i = 0; i < vc.Children.Length; ++i)
            {
                vcs.AddRange(_GetChipAndChildren(vc.Children[i]));
            }
            vcs.Add(vc);
            return vcs;
        }
        return _GetChipAndChildren(vc).ToArray();
    }

}
