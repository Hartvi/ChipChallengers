using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class VirtualModel
{
    public VirtualChip[] chips;
    public VirtualVariable[] variables;
    public string script;

    public string ToLuaString()
    {
        // Initialize string with beginning of table
        string luaCode = "{";

        // Convert chips to Lua table string
        if (chips != null)
        {
            string chipsLuaCode = VirtualChip.ArrayToLuaString(chips);
            luaCode += $"chips = {chipsLuaCode}, ";
        }

        // Convert variables to Lua table string
        if (variables != null)
        {
            string variablesLuaCode = VirtualVariable.ArrayToLuaString(variables);
            luaCode += $"variables = {variablesLuaCode}, ";
        }

        // Add script
        if (script != null)
        {
            luaCode += $"script = '{script}' ";
        }

        // Close table and return
        luaCode += "}";
        return luaCode;
    }
    public VirtualModel() { }
    public VirtualModel(string luaModel)
    {
        PRINT.print(luaModel);
        string a = "a=" + luaModel;
        var scriptObj = new Script();
        scriptObj.DoString(a);
        var luaA = scriptObj.Globals["a"];
        PRINT.print(luaA);
        //new VirtualModel();
        //return null;
    }
}

