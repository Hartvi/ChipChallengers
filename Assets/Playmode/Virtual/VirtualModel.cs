using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Linq;

public class VirtualModel
{
    public VirtualChip[] chips;
    public VirtualVariable[] variables;
    public string script;

    private VirtualVariable _SelectedVariable;

    /// <summary>
    /// Assign string variable name, returns VirtualVariable
    /// </summary>
    public VirtualVariable GetSelectedVariable() {
        if (this._SelectedVariable == null) throw new NullReferenceException($"Selected variable is null. Set it first.");
        return this._SelectedVariable;
    }

    /// <summary>
    /// Assign string variable name, get VirtualVariable later
    /// </summary>
    //public void SetSelectedVariable<T>(T value)
    //{
    //    if (value is string)
    //    {
    //        var v = this.variables.First(x => x.name == value);

    //        if (v == null)
    //        {
    //            throw new NullReferenceException($"Variable {value} doesn't exist in {this}.variables");
    //        }
    //        this._SelectedVariable = v;
    //    } else if(value is VirtualVariable)
    //    {
    //        this._SelectedVariable = value;
    //    } else
    //    {
    //        throw new TypeLoadException($"Cannot set variable from non [string, VirtualVariable] types.");
    //    }
    //}

    public void SetSelectedVariable(object value)
    {
        if (value is string stringValue)
        {
            var v = this.variables.FirstOrDefault(x => x.name == stringValue);

            if (v == null)
            {
                throw new NullReferenceException($"Variable {value} doesn't exist in {this}.variables");
            }
            this._SelectedVariable = v;
        }
        else if (value is VirtualVariable virtualVariable)
        {
            this._SelectedVariable = virtualVariable;
        }
        else
        {
            throw new TypeLoadException($"Cannot set variable from non [string, VirtualVariable] types.");
        }
    }


    private VirtualChip _SelectedChip;
    public VirtualChip SelectedChip
    {
        get
        {
            if (this._SelectedChip == null) throw new NullReferenceException($"Selected chip is null. Set it first.");

            return this._SelectedChip;
        }
        set
        {
            if (value == null) throw new ArgumentNullException($"Cannot assign null to selected chip.");

            this._SelectedChip = value;
        }
    }

    public VirtualChip Core
    {
        get
        {
            var core = this.chips.First(x => x.IsCore);
            if (core is null)
            {
                throw new NullReferenceException($"Model {this} doesn't have a core.");
            }
            return core;
        }
    }

    public string ToLuaString()
    {
        string luaCode = "{";

        if (this.chips != null)
        {
            string chipsLuaCode = VirtualChip.ArrayToLuaString(this.chips);
            luaCode += $"chips = {chipsLuaCode}, ";
        }

        if (this.variables != null)
        {
            string variablesLuaCode = VirtualVariable.ArrayToLuaString(this.variables);
            luaCode += $"variables = {variablesLuaCode}, ";
        }

        if (this.script != null)
        {
            luaCode += $"script = '{this.script}' ";
        }

        // Close table and return
        luaCode += "}";
        return luaCode;
    }

    public VirtualModel() { }
    public static VirtualModel FromLuaModel(string luaModel)
    {
        //PRINT.print(luaModel);
        string a = "a=" + luaModel;
        var scriptObj = new Script();
        scriptObj.DoString(a);
        var luaA = scriptObj.Globals["a"];
        //PRINT.print((Table)(luaA));
        return new VirtualModel((Table)luaA);
    }

    public VirtualModel(Table luaTable)
    {
        var chipsTable = luaTable.Get("chips").Table;
        if (chipsTable != null)
        {
            this.chips = chipsTable.Values.Select(t => new VirtualChip(t.Table)).ToArray();
        }

        var variablesTable = (Table)luaTable["variables"];
        if (variablesTable != null)
        {
            this.variables = VirtualVariable.FromLuaTables(variablesTable.Values.Cast<Table>().ToArray());
            // TODO VARIABLES
            PRINT.print("variables:");
            PRINT.print(this.variables);
        }

        this.script = (string)luaTable["script"];
        bool coreFound = false;
        foreach(var virtualChip in this.chips)
        {
            var parentChips = this.chips.Where(x => x.id == virtualChip.parentId).ToArray();
            if (parentChips.Length > 1)
            {
                throw new ArgumentException($"Chip {virtualChip} cannot have more than one Parent: {PRINT.MakePrintable(parentChips)}.");
            }
            else if (parentChips.Length == 0)
            {
                if (coreFound)
                {
                    throw new ArgumentException($"Cannot have more than one core. Chip {virtualChip.id} has no Parent.");
                }
                coreFound = true;
            }
            else
            {
                virtualChip.parentChip = parentChips[0];
            }
        }
    }

    public void AddVariable(VirtualVariable v)
    {
        this.variables = this.variables.Concat(new VirtualVariable[]{ v }).ToArray();
    }

    public void AddAndSelectVariable(VirtualVariable v)
    {
        this.AddVariable(v);
        this.SetSelectedVariable(v);
    }

}

