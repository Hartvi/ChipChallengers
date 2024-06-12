using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class ExtraFunctions : MonoBehaviour
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


    public static void Print(string s, Table tbl)
    {
        Vector2 position = Vector2.zero;

        var tblx = tbl.Get("x");
        var tbly = tbl.Get("y");
        position.x = tblx == DynValue.Nil ? 0f : (float)tblx.Number;
        position.y = tbly == DynValue.Nil ? 0f : (float)tbly.Number;

        var tblr = tbl.Get("r");
        float r = tblr == DynValue.Nil ? 0f : (float)tblr.Number;
        var tblg = tbl.Get("g");
        float g = tblg == DynValue.Nil ? 0f : (float)tblg.Number;
        var tblb = tbl.Get("b");
        float b = tblb == DynValue.Nil ? 0f : (float)tblb.Number;

        DisplaySingleton.Instance.DisplayText(
            txt =>
            {
                txt.transform.position = new Vector2(Screen.width * (0.5f + position.x * 0.5f), Screen.height * (0.5f + position.y * 0.5f));
                txt.color = new Color(r, g, b);
                txt.SetText(s);
            }, 0.5f
        );
    }

    public static float Sin(float x)
    {
        return Mathf.Sin(x);
    }

    public static float Cos(float x)
    {
        return Mathf.Cos(x);
    }

    public static Table Mouse(Script script)
    {
        Table tbl = new Table(script);
        var mp = Input.mousePosition;
        tbl[1] = (mp[0] / Screen.width - 0.5f) * 2f;
        tbl[2] = (mp[1] / Screen.height - 0.5f) * 2f;
        return tbl;
    }

    public static Table IsClicked(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = Input.GetMouseButton(0);
        tbl[2] = Input.GetMouseButton(1);
        return tbl;
    }

    public static Table MouseDown(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = Input.GetMouseButtonDown(0);
        tbl[2] = Input.GetMouseButtonDown(1);
        return tbl;
    }

    public static Table MouseUp(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = Input.GetMouseButtonUp(0);
        tbl[2] = Input.GetMouseButtonUp(1);
        return tbl;
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
        script.Globals["Print"] = (Action<string, Table>)Print;

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
        this.script.Globals["Mouse"] = (Func<Script, Table>)ExtraFunctions.Mouse;
        this.script.Globals["IsClicked"] = (Func<Script, Table>)ExtraFunctions.IsClicked;
        this.script.Globals["MouseDown"] = (Func<Script, Table>)ExtraFunctions.MouseDown;
        this.script.Globals["MouseUp"] = (Func<Script, Table>)ExtraFunctions.MouseUp;
        this.script.Globals["Print"] = (Action<string, Table>)ExtraFunctions.Print;
        this.script.Globals["SetVar"] = (Action<string, float>)this.SetVariable;
        this.script.Globals["GetVar"] = (Func<string, float>)this.GetVariable;

        // place holder, user can rewrite it later
        this.script.Globals["Loop"] = (Action)(() => { });

        /*
         * w1 = chip_name.ReadOmega()
         * */
        // FOR EACH CHIP:
        // register a new function as chip_nameReadOmega that returns sth
        // replace chip_name.ReadOmega() with chip_nameReadOmega()

        // select all variables
        string[] varNames = vModel.variables.Select(x => x.name).ToArray();
        // only choose valid ones
        varNames = varNames.Where(x => x.IsVariableName()).ToArray();

        this.scriptString = ScriptInstance.TransformCode(this.scriptString, varNames);

        try
        {
            this.script.DoString(this.scriptString);
        }
        catch (Exception e)
        {
            DisplaySingleton.Instance.DisplayText(
                x =>
                {
                    DisplaySingleton.ErrorMsgModification(x);
                    x.SetText(e.Message);
                },
                3f
            );
        }
    }

    /// <summary>
    /// Link sensors to real chips. VModel should have real chips assigned
    /// </summary>
    /// <param name="vModel"></param>
    public void LinkSensors(VModel vModel)
    {
        if (!vModel.hasRealChips)
        {
            throw new ArgumentNullException($"Virtual model does not have real chips initialized!");
        }

        for (int i = 0; i < vModel.chips.Length; ++i)
        {
            int _i = i;
            var currentChip = vModel.chips[_i];
            SensorAspect sa = currentChip.rChip.GetComponent<SensorAspect>();
            if (sa is null) { continue; }

            bool hasName = currentChip.TryGetProperty<string>(VChip.nameStr, out string nameval);
            string sensorStr = nameval + UIStrings.Read;
            switch (sa.sensorType)
            {
                case SensorType.Distance:
                    {

                        this.script.Globals[sensorStr] = (Func<float>)(sa.ReadDistance);
                        break;
                    }
                case SensorType.Altitude:
                    {
                        this.script.Globals[sensorStr] = (Func<float>)sa.ReadAltitude;
                        break;
                    }
                case SensorType.AngularVelocity:
                    {
                        this.script.Globals[sensorStr] = (Func<Script, Table>)sa.ReadAngularVelocity;
                        break;
                    }
                case SensorType.Rotation:
                    {
                        this.script.Globals[sensorStr] = (Func<Script, Table>)sa.ReadRotation;
                        break;
                    }
                case SensorType.Acceleration:
                    {
                        this.script.Globals[sensorStr] = (Func<Script, Table>)sa.ReadAcceleration;
                        break;
                    }
                case SensorType.Velocity:
                    {
                        this.script.Globals[sensorStr] = (Func<Script, Table>)sa.ReadVelocity;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            this.scriptString = this.scriptString.Replace("." + UIStrings.Read, UIStrings.Read);
            this.script.DoString(this.scriptString);
        }
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
        try
        {
            //foreach (var k in this.script.Globals.Keys)
            //{
            //    PRINT.IPrint($"Key: {k} Value: {this.script.Globals[k]}");
            //}
            //PRINT.IPrint($"Script:\n{this.scriptString}");
            DynValue res = script.Call(script.Globals["Loop"]);
        }
        catch (Exception e)
        {
            DisplaySingleton.Instance.DisplayText(
                x =>
                {
                    //PRINT.IPrint($"Setting compile error");
                    DisplaySingleton.ErrorMsgModification(x);
                    x.SetText(e.Message);
                }, 5f
            );
        }
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
