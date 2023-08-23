using System;
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

    public VirtualVariable()
    {
        this.name = "";
        this.minValue = -1f;
        this.maxValue = 1f;
        this.backstep = 1f;
        this.defaultValue = 0f;
    }
    public VirtualVariable(Table luaTable)
    {
        this.name = (string)luaTable["name"];
        PRINT.print($"{luaTable["minValue"].GetType()}");
        //this.minValue = float.Parse((string)luaTable["minValue"].ToString());
        //this.maxValue = float.Parse((string)luaTable["maxValue"]);
        //this.backstep = float.Parse((string)luaTable["backstep"]);
        //this.defaultValue = float.Parse((string)luaTable["defaultValue"]);
        this.minValue = float.Parse(luaTable["minValue"].ToString());
        this.maxValue = float.Parse(luaTable["maxValue"].ToString());
        this.backstep = float.Parse(luaTable["backstep"].ToString());
        this.defaultValue = float.Parse(luaTable["defaultValue"].ToString());
    }
    public static VirtualVariable[] FromLuaTables(Table[] luaTables)
    {
        return luaTables.Select(t => new VirtualVariable(t)).ToArray();
    }
    public string[] ToStringArray()
    {
        return new string[] { this.name, this.defaultValue.ToString(), this.minValue.ToString(), this.maxValue.ToString(), this.backstep.ToString() };
    }
    public VirtualVariable(string[] vals)
    {
        if (vals.Length < 5) // Ensure there are enough elements in the array
            throw new ArgumentException("vals must contain at least 5 elements");

        this.name = vals[0];
        this.defaultValue = float.Parse(vals[1]);
        this.minValue = float.Parse(vals[2]);
        this.maxValue = float.Parse(vals[3]);
        this.backstep = float.Parse(vals[4]);
    }
    public override string ToString()
    {
        return this.name;
    }
}

