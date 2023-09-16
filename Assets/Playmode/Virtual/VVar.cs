using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MoonSharp.Interpreter;

public class VVar
{
    public VModel MyModel;
    //private VModel _MyModel;
    
    //// set once to recreate the variable every tiem a new model is made/rebuilt
    //[SetOnce]
    //public VModel MyModel
    //{
    //    get { return this._MyModel; }
    //    set
    //    {
    //        if(this._MyModel is null)
    //        {
    //            this._MyModel = value;
    //        }
    //        else
    //        {
    //            throw new InvalidOperationException("The property can only be set once.");
    //        }
    //    }
    //}

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

    public static string ArrayToLuaString(VVar[] variables)
    {
        string luaCode = "{";

        foreach (VVar variable in variables)
        {
            luaCode += variable.ToLuaString() + ", ";
        }

        luaCode += "}";
        return luaCode;
    }

    public VVar()
    {
        this.name = "";
        this.minValue = -1f;
        this.maxValue = 1f;
        this.backstep = 1f;
        this.defaultValue = 0f;
    }
    public VVar(Table luaTable)
    {
        this.name = (string)luaTable["name"];
        //PRINT.print($"{luaTable["minValue"].GetType()}");
        //this.minValue = float.Parse((string)luaTable["minValue"].ToString());
        //this.maxValue = float.Parse((string)luaTable["maxValue"]);
        //this.backstep = float.Parse((string)luaTable["backstep"]);
        //this.defaultValue = float.Parse((string)luaTable["defaultValue"]);
        this.minValue = float.Parse(luaTable["minValue"].ToString());
        this.maxValue = float.Parse(luaTable["maxValue"].ToString());
        this.backstep = float.Parse(luaTable["backstep"].ToString());
        this.defaultValue = float.Parse(luaTable["defaultValue"].ToString());
    }
    public static VVar[] FromLuaTables(Table[] luaTables)
    {
        return luaTables.Select(t => new VVar(t)).ToArray();
    }
    public string[] ToStringArray()
    {
        return new string[] { this.name, this.defaultValue.ToString(), this.minValue.ToString(), this.maxValue.ToString(), this.backstep.ToString() };
    }
    public VVar(string[] vals)
    {
        if (vals.Length < 5) // Ensure there are enough elements in the array
            throw new ArgumentException("vals must contain at least 5 elements");

        this.name = vals[0];
        float defaultValue;
        if (!float.TryParse(vals[1], out defaultValue))
        {
            defaultValue = 0f;
        }

        float minValue;
        if (!float.TryParse(vals[2], out minValue))
        {
            minValue = 0f;
        }

        float maxValue;
        if (!float.TryParse(vals[3], out maxValue))
        {
            maxValue = 0f;
        }

        float backstep;
        if (!float.TryParse(vals[4], out backstep))
        {
            backstep = 0f;
        }

        this.defaultValue = defaultValue;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.backstep = backstep;

    }
    public override string ToString()
    {
        return this.name;
    }
}

