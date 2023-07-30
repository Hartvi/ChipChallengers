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

}

