using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Linq;
using Mirror;

public class VModel
{
    protected string modelName = "";
    public string ModelName { get { return this.modelName; } }

    public bool hasRealChips
    {
        get
        {
            for (int i = 0; i < this.chips.Length; ++i)
            {
                if (this.chips[i].rChip == null)
                {
                    PRINT.IPrint($"CHIP {i}: {this.chips[i].ChipType} IS NOT REAL");
                    return false;
                }
            }
            return true;
        }
    }

    public static VVar EmptyVariable
    {
        get
        {
            return new VVar();
        }
    }

    private VChip[] _chips;
    public VChip[] chips
    {
        get
        {
            return this._chips;
        }
        set
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }
            this._chips = value;
            var childrenByParent = value.ToLookup(chip => chip.parentId);
            foreach (var chip in value)
            {
                chip.Children = childrenByParent[chip.id].ToArray();
                chip.MyModel = this;
            }
            this.TriggerModelChanged();
        }
    }

    public void SetChipsWithoutNotify(VChip[] vcs)
    {
        this._chips = vcs;
    }

    private VVar[] _variables;
    public VVar[] variables
    {
        get
        {
            return this._variables;
        }
        set
        {
            // TODO test this, maybe hide the dummy variable later on
            //this._variables = value.Where(x => x.name.IsVariableName()).ToArray();
            this._variables = value;
            foreach (var v in value)
            {
                v.MyModel = this;
            }
            this.TriggerModelChanged();
        }
    }

    private string _script = @"
-- comment
-- Available functions:
-- Key(""k"") => true/false
-- KeyDown(""k"") => true/false
-- KeyUp(""k"") => true/false
-- Sin(x) => float
-- Cos(x) => float
-- You can directly set variables that you created 
function Loop()
 if Key(""w"") then
  a = 1
 end
end
";

    public string script
    {
        get
        {
            return this._script;
        }
        set
        {
            this._script = value;
            this.TriggerModelChanged();
        }
    }

    private VVar _SelectedVariable;

    private Action<string>[] SelectedActions = new Action<string>[] { };
    private Action<string>[] AddedActions = new Action<string>[] { };
    private Action<string>[] DeleteActions = new Action<string>[] { };
    private Action<VModel>[] ModelChangedActions = new Action<VModel>[] { };


    private VChip _SelectedChip;
    public VChip SelectedChip
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

    public VChip Core
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
            string chipsLuaCode = VChip.ArrayToLuaString(this.chips);
            luaCode += $"chips = {chipsLuaCode}, ";
        }

        if (this.variables != null)
        {
            string variablesLuaCode = VVar.ArrayToLuaString(this.variables);
            luaCode += $"variables = {variablesLuaCode}, ";
        }

        if (this.script != null)
        {
            luaCode += $"script = {LuaStrings.Quote(this.script)} ";
        }

        // Close table and return
        luaCode += "}";
        return luaCode;
    }

    public VModel()
    {
        this.variables = new VVar[] { VModel.EmptyVariable };
    }

    public static VModel FromLuaModel(string luaModel)
    {
        return new VModel(LuaModelData.Parse(luaModel));
    }

    public VModel(Table luaTable)
    {
        ModelValidation.Validate(luaTable,
            ArrayExtensions.ToDictionaryFromArrays(VChip.chipData.keys, VChip.chipData.values),
            ArrayExtensions.ToDictionaryFromArrays(VChip.optionNames.keys, VChip.optionNames.values.Select(values => values.Length).ToArray()),
            VChip.chipNames);
        var chipsTable = luaTable.Get("chips").Table;
        if (chipsTable != null)
        {
            this.chips = chipsTable.Values.Select(t => new VChip(t.Table)).ToArray();
        }
        else
        {
            throw new NullReferenceException($"Chips is null!");
        }

        var variablesTable = (Table)luaTable["variables"];
        if (variablesTable != null)
        {
            //PRINT.print(variablesTable.Values.Count());
            this.variables = VVar.FromLuaTables(variablesTable.Values.Select(x => x.Table).ToArray());
            // TODO VARIABLES
            //PRINT.IPrint("todo: variables:");
            //PRINT.IPrint(this.variables);
        }
        else
        {
            throw new NullReferenceException($"Variables is null!");
        }

        this.script = (string)luaTable["script"];
        // Resolve new property bindings before spawning; construction must not trigger nested model loads.
        foreach (var chip in this.chips)
        {
            for (int i = 0; i < chip.keys.Length; ++i)
            {
                string key = chip.keys[i], value = chip.vals[i];
                if (!VChip.dynamicPropertiesStr.Contains(key) || !ModelValidation.IsVariableName(value) ||
                    (key == VChip.colourStr && StringHelpers.IsColourString(value)) || this.variables.Any(variable => variable.name == value))
                    continue;
                if (this.variables.Length >= ModelValidation.MaximumVariables) { throw new ArgumentException("Too many model variables."); }
                this.variables = this.variables.Append(VVar.DefaultValueVariable(value)).ToArray();
            }
        }
        var chipsById = this.chips.ToDictionary(chip => chip.id, StringComparer.Ordinal);
        foreach (var chip in this.chips)
            chip.parentChip = chip.parentId != null && chipsById.TryGetValue(chip.parentId, out var parent) ? parent : null;
    }

    public void AddModelChangedCallback(Action<VModel> action)
    {
        //PRINT.print($"Number of callbacks: {this.ModelChangedActions.Length}");
        this.ModelChangedActions = this.ModelChangedActions.AddWithoutDuplicate(action);
        // add variable, add chip, change variable, change chip
    }

    public void TriggerModelChanged()
    {
        foreach (var action in this.ModelChangedActions)
        {
            action(this);
        }
    }

    public void AddVariable(VVar v)
    {
        VVar existingVariable = this.variables.FirstOrDefault(x => x.name == v.name);

        if (existingVariable is null)
        {
            this.variables = this.variables.Concat(new VVar[] { v }).ToArray();
        }
        else
        {
            int i = Array.IndexOf(this.variables, existingVariable);
            VVar[] varArr = this.variables.Where((x, y) => y != i).ToArray();
            VVar[] newVarArr = varArr.Concat(new VVar[] { v }).ToArray();
            this.variables = newVarArr;
            // TODO TEST THIS
        }

        PRINT.IPrint($"Num add listeners: {this.AddedActions.Length}");
        foreach (var action in this.AddedActions)
        {
            action(v.name);
        }
    }

    public void AddAndSelectVariable(VVar v)
    {
        this.AddVariable(v);
        this.SetSelectedVariable(v.name);
    }

    public VVar GetSelectedVariable()
    {
        //if (this._SelectedVariable == null) throw new NullReferenceException($"Selected variable is null. Set it first.");
        return this._SelectedVariable;
    }

    public void DeleteSelectedVariable()
    {
        VVar selectedVar = this.GetSelectedVariable();
        if (selectedVar == null) { return; }
        string selectedVarName = selectedVar.name;
        if (string.IsNullOrEmpty(selectedVarName)) { return; }

        // Read stored strings directly: missing properties can have numeric defaults.
        if (this.chips.Any(chip => Enumerable.Range(0, chip.keys.Length).Any(index =>
            VChip.dynamicPropertiesStr.Contains(chip.keys[index]) && chip.vals[index] == selectedVarName)))
        {
            DisplaySingleton.Instance.DisplayText(text =>
            {
                DisplaySingleton.ErrorMsgModification(text);
                text.SetText("variable is still being used in a chip");
            }, 3f);
            return;
        }

        this.variables = this.variables.Where(x => x != selectedVar).ToArray(); //  .Concat(new VirtualVariable[]{ v }).ToArray();
        this.SetSelectedVariable(string.Empty);

        foreach (var action in this.DeleteActions)
        {
            action(selectedVarName);
        }
    }

    public void SetSelectedVariable(string value)
    {
        PRINT.IPrint($"Selecting variable {value}");
        var NonNullVar = this.variables.FirstOrDefault(x => x.name == value);

        if (NonNullVar is null)
        {
            //throw new NullReferenceException($"Variable {value} doesn't exist in {this}.variables");
            this._SelectedVariable = VModel.EmptyVariable;
        }
        else
        {
            this._SelectedVariable = NonNullVar;
        }


        foreach (var action in this.SelectedActions)
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

    // RemoveAddedVariableListener removes a callback for variable additions.
    public void RemoveAddedVariableListener(Action<string> action)
    {
        this.AddedActions = this.AddedActions.Where(x => x != action).ToArray();
    }

    // RemoveDeleteVariableListener removes a callback for variable deletions.
    public void RemoveDeleteVariableListener(Action<string> action)
    {
        this.DeleteActions = this.DeleteActions.Where(x => x != action).ToArray();
    }

    // RemoveSetSelectedVariableListener removes a callback for variable selection changes.
    public void RemoveSetSelectedVariableListener(Action<string> action)
    {
        this.SelectedActions = this.SelectedActions.Where(x => x != action).ToArray();
    }

    [Client]
    public string SaveThisModelToFile(string modelName)
    {
        var modelLua = this.ToLuaString();
        var snapshot = VModel.FromLuaModel(modelLua);
        VehicleLuaProgram.ValidateSyntax(snapshot.script);

        IOHelpers.SaveModel(modelName, modelLua);
        return null;
    }

    public static VModel LoadModelFromFile(string modelName)
    {
        if (!IOHelpers.ModelExists(modelName))
        {
            return null;
        }

        string luaModel = IOHelpers.LoadModel(modelName);
        VModel m = VModel.FromLuaModel(luaModel);
        m.modelName = modelName.Substring(0, modelName.Length - UIStrings.ModelExtension.Length);
        //PRINT.IPrint(m.variables.Length);
        return m;
    }

    public VChip[] GetAllVChips()
    {
        return GetChipAndChildren("a");
    }

    VChip[] GetChipAndChildren(string id)
    {
        var root = this.chips.FirstOrDefault(chip => chip.id == id);
        if (root == null) { return Array.Empty<VChip>(); }
        var result = new List<VChip>();
        var pending = new Stack<VChip>();
        var visited = new HashSet<VChip>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var chip = pending.Pop();
            if (!visited.Add(chip)) { continue; }
            result.Add(chip);
            foreach (var child in chip.Children) { pending.Push(child); }
        }
        return result.ToArray();
    }

    public void DeleteChip(string id)
    {
        VChip[] chipsToDelete = this.GetChipAndChildren(id).ToArray();
        // vcs is the chips we DONT want to delete
        this.chips = Array.FindAll(this.chips, x => !chipsToDelete.Contains(x));
    }

}
