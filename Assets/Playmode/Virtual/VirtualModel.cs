using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Linq;

public class VirtualModel
{
    public static VirtualVariable EmptyVariable
    {
        get
        {
            return new VirtualVariable();
        }
    }

    public VirtualChip[] chips;
    public VirtualVariable[] variables;
    public string script;

    private VirtualVariable _SelectedVariable;

    private Action<string>[] SelectedActions = new Action<string>[] { };
    private Action<string>[] AddedActions = new Action<string>[] { };
    private Action<string>[] DeleteActions = new Action<string>[] { };


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

    public VirtualModel()
    {
        this.variables = new VirtualVariable[] { VirtualModel.EmptyVariable };
        //this.chips
    }

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
            //PRINT.print(variablesTable.Values.Count());
            this.variables = VirtualVariable.FromLuaTables(variablesTable.Values.Select(x => x.Table).ToArray());
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
        VirtualVariable existingVariable = this.variables.FirstOrDefault(x => x.name == v.name);

        if (existingVariable is null)
        {
            this.variables = this.variables.Concat(new VirtualVariable[] { v }).ToArray();
        }
        else
        {
            int i = Array.IndexOf(this.variables, existingVariable);
            VirtualVariable[] varArr = this.variables.Where((x, y) => y != i).ToArray();
            VirtualVariable[] newVarArr = varArr.Concat(new VirtualVariable[] { v }).ToArray();
            this.variables = newVarArr;
            // TODO TEST THIS
        }

        foreach(var action in this.AddedActions)
        {
            action(v.name);
        }
    }

    public void AddAndSelectVariable(VirtualVariable v)
    {
        this.AddVariable(v);
        this.SetSelectedVariable(v.name);
    }

    public VirtualVariable GetSelectedVariable()
    {
        if (this._SelectedVariable == null) throw new NullReferenceException($"Selected variable is null. Set it first.");
        return this._SelectedVariable;
    }

    public void DeleteSelectedVariable()
    {
        VirtualVariable selectedVar = this.GetSelectedVariable();
        string selectedVarName = selectedVar.name;

        this.variables = this.variables.Where(x => x != selectedVar).ToArray(); //  .Concat(new VirtualVariable[]{ v }).ToArray();
        this.SetSelectedVariable(string.Empty);
        
        foreach(var action in this.DeleteActions)
        {
            action(selectedVarName);
        }
    }

    public void SetSelectedVariable(string value)
    {
        PRINT.print($"Selecting variable {value}");
        var NonNullVar = this.variables.FirstOrDefault(x => x.name == value);

        if (NonNullVar is null)
        {
            //throw new NullReferenceException($"Variable {value} doesn't exist in {this}.variables");
            this._SelectedVariable = VirtualModel.EmptyVariable;
        }
        else
        {
            this._SelectedVariable = NonNullVar;
        }


        foreach(var action in this.SelectedActions)
        {
            action(value);
        }
    }

    public void AddSetSelectedVariableListener(Action<string> action)
    {
        this.SelectedActions = this.SelectedActions.Concat(new Action<string>[] { action }).ToArray();
    }

    public void AddAddedVariableListener(Action<string> action)
    {
        this.AddedActions = this.AddedActions.Concat(new Action<string>[] { action }).ToArray();
    }

    public void AddDeleteVariableListener(Action<string> action)
    {
        this.DeleteActions = this.DeleteActions.Concat(new Action<string>[] { action }).ToArray();
    }

}

