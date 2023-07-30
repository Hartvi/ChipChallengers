using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MoonSharp.Interpreter;

public class VirtualVariable
{
    public string name;
    public float defaultValue, maxValue, minValue, backstep;

    public string ToLuaString()
    {
        // Initialize string with beginning of table
        string luaCode = "{";

        // Add fixed fields
        luaCode += $"name = '{name}', ";
        luaCode += $"defaultValue = {defaultValue}, ";
        luaCode += $"maxValue = {maxValue}, ";
        luaCode += $"minValue = {minValue}, ";
        luaCode += $"backstep = {backstep}, ";

        // Close table and return
        luaCode += "}";
        return luaCode;
    }

    public static string ArrayToLuaString(VirtualVariable[] variables)
    {
        string luaCode = "{";

        foreach (VirtualVariable variable in variables)
        {
            luaCode += variable.ToLuaString() + ", ";
        }

        luaCode += "}";
        return luaCode;
    }

    public VirtualVariable(Table luaTable)
    {
        this.name = (string)luaTable["name"];
        this.minValue = float.Parse((string)luaTable["minValue"]);
        this.maxValue = float.Parse((string)luaTable["maxValue"]);
        this.backstep = float.Parse((string)luaTable["backstep"]);
        this.defaultValue = float.Parse((string)luaTable["defaultValue"]);
    }
    public static VirtualVariable[] FromLuaTables(Table[] luaTables)
    {
        return luaTables.Select(t => new VirtualVariable(t)).ToArray();
    }

}
