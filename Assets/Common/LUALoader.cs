using System;
using System.Collections;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChipData
{
    public string[] keys;
    public string[][] values;
}


public static class LUALoader
{
    public static ChipData LoadChipProperties(string filePath)
    {
        string luaCode = IOHelpers.LoadTextFile(filePath);
        //PRINT.print($"lua code:\n{luaCode}");
        Script script = new Script();
        script.DoString(luaCode);

        DynValue luaData = script.Globals.Get("a");

        string[] chips = ConvertTableToArray(luaData.Table.Get("keys").Table, x => x.String).Cast<string>().ToArray();

        string[][] properties = ConvertTableToArray(luaData.Table.Get("values").Table, x => x.String).Select(x => ((object[])x).Cast<string>().ToArray()).ToArray();

        ChipData cd = new ChipData();
        cd.keys = chips;
        cd.values = properties;
        return cd;
    }

    private static object[] ConvertTableToArray(Table table, Func<DynValue, object> f)
    {
        List<object> ret = new List<object>();
        foreach(var v in table.Values)
        {
            if (v.Type == DataType.Table)
            {
                ret.Add(ConvertTableToArray(v.Table, f));
            }
            else
            {
                break;
            }
        }

        if(table.Values.All(x => f(x) is not null))
        {
            return table.Values.Select(f).ToArray();
        }
        else
        {
            return ret.ToArray();
        }
    }

}
