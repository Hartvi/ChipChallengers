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
        return Input.GetKey(InputHelper.chartoKeycode[k]);
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

        // place holder, user can rewrite it later
        this.script.Globals["Loop"] = (Action)(() => { });

        //PRINT.print($"Script string: is null: {this.scriptString == null}, {this.scriptString}");
        this.script.DoString(this.scriptString);
    }

    public void CallLoop()
    {
        DynValue res = script.Call(script.Globals["Loop"]);
    }

    public void SetVariable(string var, float val)
    {
        //PRINT.print($"Settings variable {var} to value {val} from {this.Name2Var[var].currentValue}");
        this.Name2Var[var].currentValue = val;
        //PRINT.print($"setting value succeeded");
    }
}
