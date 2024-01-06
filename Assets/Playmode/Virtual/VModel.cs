using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using System.Linq;

public class VModel
{
    protected string modelName = "";
    public string ModelName { get { return this.modelName; } }

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
            //PRINT.print($"Setting chips to length: {value.Length}");
            this._chips = value;
            for(int i = 0; i < this._chips.Length; ++i)
            {
                // TODO link children to parents since we have all chips at once so we can quickly build the model
                VChip chip = this._chips[i];
                //PRINT.print($"Id: {chip.id}");
                for (int k = 0; k < this._chips.Length; ++k)
                {
                    VChip[] children = this._chips.Where(x => x.parentId == chip.id).ToArray();
                    chip.Children = children;
                }
            }
            foreach(var v in value)
            {
                v.MyModel = this;
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
            luaCode += $"script = [[{this.script}]] ";
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
        //PRINT.print(luaModel);
        string a = "a=" + luaModel;
        var scriptObj = new Script();
        scriptObj.DoString(a);
        var luaA = scriptObj.Globals["a"];
        //PRINT.print($"MODEL:");
        //PRINT.print((Table)(luaA));
        return new VModel((Table)luaA);
    }

    public VModel(Table luaTable)
    {
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
            PRINT.IPrint("todo: variables:");
            PRINT.IPrint(this.variables);
        }
        else
        {
            throw new NullReferenceException($"Variables is null!");
        }

        this.script = (string)luaTable["script"];
        PRINT.IPrint($"LOADED SCRIPT: {this.script}");
        bool coreFound = false;
        //PRINT.print($"Number of chips: {this.chips.Length}");
        //foreach (var virtualChip in this.chips)
        //{
        //    PRINT.print($"Id: {virtualChip.id}");
        //    PRINT.print($"parent id: {virtualChip.parentId}");
        //}
        foreach(var virtualChip in this.chips)
        {
            var parentChips = this.chips.Where(x => x.id == virtualChip.parentId).ToArray();
            if (parentChips.Length > 1)
            {
                throw new ArgumentException($"Chip {virtualChip} cannot have more than one parent, id: {virtualChip.parentId}, num: {PRINT.MakePrintable(parentChips)}.");
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

    public void AddModelChangedCallback(Action<VModel> action)
    {
        //PRINT.print($"Number of callbacks: {this.ModelChangedActions.Length}");
        this.ModelChangedActions = this.ModelChangedActions.AddWithoutDuplicate(action);
        // add variable, add chip, change variable, change chip
    }

    public void TriggerModelChanged()
    {
        //PRINT.print($"TRIGGER MODEL CHANGED");
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
        foreach(var action in this.AddedActions)
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
        if (this._SelectedVariable == null) throw new NullReferenceException($"Selected variable is null. Set it first.");
        return this._SelectedVariable;
    }

    public void DeleteSelectedVariable()
    {
        VVar selectedVar = this.GetSelectedVariable();
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

    public string SaveThisModelToFile(string modelName)
    {
        var modelLua = this.ToLuaString();

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
        PRINT.IPrint(m.variables.Length);
        return m;
    }

    public VChip[] GetAllVChips() {
        return GetChipAndChildren("a");
    }

    VChip[] GetChipAndChildren(string id)
    {
        CommonChip core = CommonChip.ClientCore;
        VModel vm = core.VirtualModel;
        var allchips = core.AllChips;

        List<VChip> _GetChipAndChildren(string id)
        {
            List<VChip> vcs = new List<VChip>();
            CommonChip cc = allchips.FirstOrDefault(x => x.equivalentVirtualChip.id == id) as CommonChip;

            VChip vc = cc.equivalentVirtualChip;
            for (int i = 0; i < vc.Children.Length; ++i)
            {
                vcs.AddRange(_GetChipAndChildren(vc.Children[i].id));
            }
            vcs.Add(vc);
            return vcs;
        }
        return _GetChipAndChildren(id).ToArray();
    }

    public void DeleteChip(string id) {
        VChip[] chipsToDelete = this.GetChipAndChildren(id).ToArray();
        // vcs is the chips we DONT want to delete
        this.chips = Array.FindAll(this.chips, x => !chipsToDelete.Contains(x));
    }

}
