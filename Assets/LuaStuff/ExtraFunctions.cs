using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;


public class ScriptInstance
{
    CoreChip myCore;
    InputMessage inputMessage => myCore.inputMessage;

    Script script;
    string scriptString;

    Dictionary<string, VVar> Name2Var;

    public ScriptInstance(CoreChip cc, VModel vModel)
    {
        this.myCore = cc;
        this.Name2Var = vModel.variables.ToDictionary(x => x.name, x => x);
        this.scriptString = vModel.script == null ? "" : vModel.script;

        this.script = new Script();

        // USER INPUTS
        this.script.Globals[UIStrings.Key] = (Func<char, bool>)this.Key;
        this.script.Globals[UIStrings.KeyDown] = (Func<char, bool>)this.KeyDown;
        this.script.Globals[UIStrings.KeyUp] = (Func<char, bool>)this.KeyUp;
        this.script.Globals["Mouse"] = (Func<Script, Table>)this.Mouse;
        this.script.Globals["IsClicked"] = (Func<Script, Table>)this.IsClicked;
        this.script.Globals["MouseDown"] = (Func<Script, Table>)this.MouseDown;
        this.script.Globals["MouseUp"] = (Func<Script, Table>)this.MouseUp;

        // GENERAL
        this.script.Globals["Sin"] = (Func<float, float>)this.Sin;
        this.script.Globals["Cos"] = (Func<float, float>)this.Cos;
        this.script.Globals["Print"] = (Action<string, Table>)this.Print;
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
#if UNITY_EDITOR
        DynValue res = script.Call(script.Globals["Loop"]);
#else
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
#endif
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

    public bool Key(char k)
    {
        return this.inputMessage.Keys.Contains(k);
        //bool ret = Input.GetKey(InputHelper.chartoKeycode[k]);
        //return ret;
    }

    public bool KeyDown(char k)
    {
        return this.inputMessage.KeysDown.Contains(k);
        //return Input.GetKeyDown(InputHelper.chartoKeycode[k]);
    }

    public bool KeyUp(char k)
    {
        return this.inputMessage.KeysUp.Contains(k);
        //return Input.GetKeyUp(InputHelper.chartoKeycode[k]);
    }

    public Table Mouse(Script script)
    {
        Table tbl = new Table(script);
        var mp = this.inputMessage.MousePos;
        tbl[1] = (mp[0] / Screen.width - 0.5f) * 2f;
        tbl[2] = (mp[1] / Screen.height - 0.5f) * 2f;
        return tbl;
    }

    public Table IsClicked(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = this.inputMessage.MouseClicked[0];
        tbl[2] = this.inputMessage.MouseClicked[1];
        tbl[3] = this.inputMessage.MouseClicked[2];
        return tbl;
    }

    public Table MouseDown(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = this.inputMessage.MouseDown[0];
        tbl[2] = this.inputMessage.MouseDown[1];
        tbl[3] = this.inputMessage.MouseDown[2];
        return tbl;
    }

    public Table MouseUp(Script script)
    {
        Table tbl = new Table(script);
        tbl[1] = this.inputMessage.MouseUp[0];
        tbl[2] = this.inputMessage.MouseUp[1];
        tbl[3] = this.inputMessage.MouseUp[2];
        return tbl;
    }

    public void Print(string s, Table tbl)
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

    public float Sin(float x)
    {
        return Mathf.Sin(x);
    }

    public float Cos(float x)
    {
        return Mathf.Cos(x);
    }
}
