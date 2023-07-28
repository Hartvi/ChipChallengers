using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualChipTest : MonoBehaviour
{
    void Start()
    {
        string[] keys1 = new string[] { "Name", "Angle", "Value", "Option", "Type" };
        string[] vals1 = new string[] { "joe", "40", "5", "1", "Core" };
        string[] keys = new string[] { "Name", "Angle", "Value", "Option", "Type" };
        string[] vals = new string[] { "joe", "20", "5", "1", "Chip" };
        string[] keys2 = new string[] { "Name", "Angle", "Value", "Option", "Type", "Spring", "Damper" };
        string[] vals2 = new string[] { "joe", "20", "5", "1", "Chip", "10", "10" };
        var vc = new VirtualChip(keys1, vals1, 0, null);
        //PRINT.print("vckeys", vc.keys);
        //PRINT.print("vcvals", vc.vals);
        //PRINT.print("vcobjectVals", vc.objectVals);
        var vc2 = new VirtualChip(keys, vals, 0, vc);
        if(vc2.id != "aa") {
            throw new KeyNotFoundException("single child of a must be aa.");
        }
        var vc3 = new VirtualChip(keys, vals, 1, vc);
        if(vc3.id != "ab") {
            throw new KeyNotFoundException("second child of a must be ab.");
        }
        var vc4 = new VirtualChip(keys, vals, 0, vc2);
        if(vc4.id != "aaa") {
            throw new KeyNotFoundException("first grandchild of a must be aaa.");
        }
        var vc5 = new VirtualChip(keys2, vals2, 0, vc4);
        var vc6 = new VirtualChip(keys2, vals2, 1, vc4);
        var vc7 = new VirtualChip(keys2, vals2, 2, vc4);
        var vc8 = new VirtualChip(keys2, vals2, 3, vc4);
        var coreobject = GameObject.Find("coretest");
        var core = coreobject.GetComponent<CommonChip>();
        //print("core name: "+core.name);
        core.TriggerSpawn(vc);
    }
}
