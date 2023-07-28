using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
