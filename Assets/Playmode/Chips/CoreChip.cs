using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreChip : CommonChip
{
    // Start is called before the first frame update
    void Start()
    {
        string[] keys1 = new string[] { VChip.nameStr, VChip.typeStr};
        string[] vals1 = new string[] { VChip.coreStr, VChip.coreStr };
        var vc = new VChip(keys1, vals1, 0, null);
        var VirtualModel = new VModel();
        VirtualModel.chips = new VChip[] { vc };//, vc2, vc3, vc4, vc5, vc6, vc7, vc8 };
        this.VirtualModel = VirtualModel;
        //print($"Core equivalent chip is NULL: {core.equivalentVirtualChip is null}");
        //core.equivalentVirtualChip = vc;
        //print("core name: "+core.name);

        TextAsset textFile = Resources.Load<TextAsset>("aguncar");
        LoadPanel.LoadString(textFile.text);

        //VModel VirtualModel = VModel.FromLuaModel(textFile.text);
        
        ////var VirtualModel = new VModel();
        ////VirtualModel.chips = new VChip[] { vc };//, vc2, vc3, vc4, vc5, vc6, vc7, vc8 };

        ////VirtualModel.AddModelChangedCallback(x => core.TriggerSpawn(x, true));

        //core.VirtualModel = VirtualModel;
        //HistoryStack.SaveState(core.VirtualModel.ToLuaString());

        //print($"Core model: {core.VirtualModel}");
        //VirtualModel.AddModelChangedCallback(x => HistoryStack.SaveState(VirtualModel.ToLuaString()));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
