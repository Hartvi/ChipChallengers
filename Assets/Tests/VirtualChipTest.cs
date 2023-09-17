using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class VirtualChipTest : MonoBehaviour
{
    bool testedOnce = false;
    void Start()
    {
        if (testedOnce) return;
        var coreobject = GameObject.Find(UIStrings.Core);
        if (coreobject == null)
        {
            print($"{this} CORE IS NULL");
        }

        string[] keys1 = new string[] { "Name", "Angle", "Value", "Option", "Type" };
        string[] vals1 = new string[] { "joe", "40", "5", "1", "Core" };
        string[] keys = new string[] { "Name", "Angle", "Value", "Option", "Type" };
        string[] vals = new string[] { "joe", "20", "5", "1", "Chip" };
        string[] keys2 = new string[] { "Name", "Angle", "Value", "Option", "Type", "Spring", "Damper" };
        string[] vals2 = new string[] { "joe", "20", "5", "1", "Axle", "100000", "10" };
        var vc = new VChip(keys1, vals1, 0, null);
        //PRINT.print("vckeys", vc.keys);
        //PRINT.print("vcvals", vc.vals);
        //PRINT.print("vcobjectVals", vc.objectVals);
        var vc2 = new VChip(keys, vals, 0, vc);
        if (vc2.id != "aa")
        {
            throw new KeyNotFoundException("single child of a must be aa.");
        }
        var vc3 = new VChip(keys, vals, 1, vc);
        if (vc3.id != "ab")
        {
            throw new KeyNotFoundException("second child of a must be ab.");
        }
        var vc4 = new VChip(keys, vals, 0, vc2);
        if (vc4.id != "aaa")
        {
            throw new KeyNotFoundException("first grandchild of a must be aaa.");
        }
        var vc5 = new VChip(keys2, vals2, 0, vc4);
        var vc6 = new VChip(keys2, vals2, 1, vc4);
        var vc7 = new VChip(keys2, vals2, 2, vc4);
        var vc8 = new VChip(keys2, vals2, 3, vc4);

        CommonChip core = coreobject.GetComponent<CommonChip>();
        //print("core name: "+core.name);

        var VirtualModel = new VModel();
        VirtualModel.chips = new VChip[] { vc, vc2, vc3, vc4, vc5, vc6, vc7, vc8 };

        VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));

        core.TriggerSpawn(VirtualModel, false);

        string vc8_str = "a=" + vc8.ToLuaString();
        print("lua string: " + vc8_str);
        var sc = new Script();
        sc.DoString(vc8_str);
        print("globals a: " + sc.Globals["a"]);
        this.testedOnce = true;
    }
}
