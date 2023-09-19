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

    public Action<float>[] valueChangedCallbacks = { };
    public float _currentValue = 0f;
    public float currentValue
    {
        get { return this._currentValue; }
        set
        {
            this._currentValue = Mathf.Max(Mathf.Min(value, this.maxValue), this.minValue);
            //PRINT.print($"Setting current value {this._currentValue} of variable {this.name}");

            foreach (Action<float> a in this.valueChangedCallbacks)
            {
                //UnityEngine.Debug.LogWarning($"TODO: check if in playmode this is setting the correct angle");
                // by default, building with default angle resets target vector to be 0 at the default angle,
                // which means we might have to subtract the default angle when setting the angle;
                // NOT to be confused with non-angle instances
                a(this._currentValue);
            }
        }
    }

    public void SetValueChangedCallbacks(Action<float>[] actions)
    {
        this.valueChangedCallbacks = actions;
    }

    public void AddValueChangedCallback(Action<float> action)
    {
        this.valueChangedCallbacks = this.valueChangedCallbacks.Append(action).ToArray();
    }

    public static VVar DefaultValueVariable(string name) {
        return new VVar(new string[] { name, "0", "-1", "1", "1" });
    }

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
        this.currentValue = 0f;
    }
    public VVar(Table luaTable)
    {
        //PRINT.print(luaTable);
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
        this.currentValue = this.defaultValue;
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
        this.currentValue = defaultValue;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.backstep = backstep;

    }
    public override string ToString()
    {
        return this.name;
    }
}

