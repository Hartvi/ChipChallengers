using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class ExtraFunctions: MonoBehaviour
{
    public static bool Key(char k)
    {
        bool ret = Input.GetKey(InputHelper.chartoKeycode[k]);
        //if (ret) print($"Getting key: {k}");
        return ret;
    }

    public static bool KeyDown(char k)
    {
        return Input.GetKeyDown(InputHelper.chartoKeycode[k]);
    }

    public static bool KeyUp(char k)
    {
        return Input.GetKeyUp(InputHelper.chartoKeycode[k]);
    }


    public static void Print(string s)
    {
        UnityEngine.Debug.Log(s);
    }

    public static float Sin(float x)
    {
        return Mathf.Sin(x);
    }

    public static float Cos(float x)
    {
        return Mathf.Cos(x);
    }


    public void TestFuncs()
    {
        string scriptCode = @"    
        -- comment
        function loop ()
            --Print(Key('a'));
        end";

        Script script = new Script();

        script.Globals["Key"] = (Func<char, bool>)Key;
        script.Globals["KeyDown"] = (Func<char, bool>)KeyDown;
        script.Globals["KeyUp"] = (Func<char, bool>)KeyUp;
        script.Globals["Sin"] = (Func<float, float>)Sin;
        script.Globals["Cos"] = (Func<float, float>)Cos;
        script.Globals["Print"] = (Action<string>)Print;

        script.DoString(scriptCode);

        DynValue res = script.Call(script.Globals["loop"]);
    }
    

    void Update()
    {
        TestFuncs();
    }

}

public class ScriptInstance
{
    Script script;
    string scriptString;

    Dictionary<string, VVar> Name2Var;

    public ScriptInstance(VModel vModel)
    {
        this.Name2Var = vModel.variables.ToDictionary(x => x.name, x => x);
        this.scriptString = vModel.script == null ? "" : vModel.script;

        this.script = new Script();

        this.script.Globals["Key"] = (Func<char, bool>)ExtraFunctions.Key;
        this.script.Globals["KeyDown"] = (Func<char, bool>)ExtraFunctions.KeyDown;
        this.script.Globals["KeyUp"] = (Func<char, bool>)ExtraFunctions.KeyUp;
        this.script.Globals["Sin"] = (Func<float, float>)ExtraFunctions.Sin;
        this.script.Globals["Cos"] = (Func<float, float>)ExtraFunctions.Cos;
        this.script.Globals["Print"] = (Action<string>)ExtraFunctions.Print;
        this.script.Globals["SetVar"] = (Action<string, float>)this.SetVariable;
        this.script.Globals["GetVar"] = (Func<string, float>)this.GetVariable;

        // place holder, user can rewrite it later
        this.script.Globals["Loop"] = (Action)(() => { });

        //PRINT.print($"Script string: is null: {this.scriptString == null}, {this.scriptString}");
        // select all variables
        string[] varNames = vModel.variables.Select(x => x.name).ToArray();
        // only choose valid ones
        varNames = varNames.Where(x=>x.IsVariableName()).ToArray();
        PRINT.IPrint($"Number of variables: {varNames.Length}");
        for(int i = 0; i < varNames.Length; ++i)
        {
            PRINT.IPrint($"variable: {varNames[i]}, is variable: {varNames[i].IsVariableName()}");
        }
        
        this.scriptString = ScriptInstance.TransformCode(this.scriptString, varNames);
        
        PRINT.IPrint($"Script string:");
        PRINT.IPrint(this.scriptString);
        
        this.script.DoString(this.scriptString);
    }
    public static string TransformCode(string code, string[] vars)
    {
        // Check if vars array is empty
        if (vars.Length == 0)
        {
            return code;
        }

        // Prepare the joined variable names for regex
        string joinedVars = string.Join("|", vars);

        // 1. Replace assignments: varName = value
        string patternAssignment = $@"\b({joinedVars})\s*=\s*([^;\n]+)";
        string replacementAssignment = @"SetVar(""$1"", $2);"; // Close SetVar here
        code = Regex.Replace(code, patternAssignment, replacementAssignment);

        // 2. Replace variable accesses: varName
        // Ensure it's not followed by a closing quote and bracket or surrounded by quotes
        string patternAccess = $@"(?<![""'])\b({joinedVars})\b(?!\s*""\s*\))(?!\s*'\s*\))";
        string replacementAccess = @"GetVar(""$1"")";
        code = Regex.Replace(code, patternAccess, replacementAccess);

        return code;
    }

    public void CallLoop()
    {
        DynValue res = script.Call(script.Globals["Loop"]);
    }

    public void SetVariable(string var, float val)
    {
        this.Name2Var[var].currentValue = val;
        this.Name2Var[var].hasChanged = true;
    }

    public float GetVariable(string var)
    {
        return this.Name2Var[var].currentValue;
    }
}
