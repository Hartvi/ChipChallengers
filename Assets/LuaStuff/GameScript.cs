using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public partial class GameScript : MonoBehaviour
{
    Script script;
    string scriptString;

    Dictionary<string, GameObject> SceneObjects = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> AllObjects = new Dictionary<string, GameObject>();
    HashSet<string> PlayerObjects = new HashSet<string>();
    Dictionary<string, double> times = new Dictionary<string, double>();
    bool isRace = false;

    public void Activate(string scriptString, string scenarioFileName = "", byte[] scenarioContents = null)
    {
        // add owner player
        string player1 = UIStrings.Player + "1";
        AllObjects.Add(player1, CoreChip.ClientCoreChip.gameObject);
        PlayerObjects.Add(player1);

        this.scriptString = scriptString;
        this.script = new Script();

        this.script.Globals["Key"] = (Func<char, bool>)Key;
        this.script.Globals["KeyDown"] = (Func<char, bool>)KeyDown;
        this.script.Globals["KeyUp"] = (Func<char, bool>)KeyUp;
        this.script.Globals["Sin"] = (Func<float, float>)Sin;
        this.script.Globals["Cos"] = (Func<float, float>)Cos;
        this.script.Globals["Mouse"] = (Func<Script, Table>)Mouse;
        this.script.Globals["IsClicked"] = (Func<Script, Table>)IsClicked;
        this.script.Globals["MouseDown"] = (Func<Script, Table>)MouseDown;
        this.script.Globals["MouseUp"] = (Func<Script, Table>)MouseUp;
        this.script.Globals["Print"] = (Action<string, Table>)Print;
        this.script.Globals["LoadMap"] = (Action<string>)LoadMap;
        this.script.Globals["SetPosition"] = (Action<string, Table>)SetPosition;
        this.script.Globals["GetPosition"] = (Func<string, float[]>)GetPosition;
        this.script.Globals["SetRotation"] = (Action<string, Table>)SetRotation;
        this.script.Globals["Spawn"] = (Func<string, Table, string>)Spawn;
        this.script.Globals["Hide"] = (Action<string>)Hide;
        this.script.Globals["Show"] = (Action<string>)Show;
        this.script.Globals["StartTimer"] = (Action<string>)StartTimer;
        this.script.Globals["GetTime"] = (Func<string, double>)GetTime;
        this.RegisterScenarioCallbacks(scenarioFileName, scenarioContents ?? System.Text.Encoding.UTF8.GetBytes(scriptString));
        this.script.Globals["Freeze"] = (Action<string>)Freeze;
        this.script.Globals["Unfreeze"] = (Action<string>)Unfreeze;
        // place holder, user can rewrite it later
        this.script.Globals["Loop"] = (Action)(() => { });

        try
        {
            CoreChip.ClientCoreChip.CmdResetToDefaultLocation();
            this.script.DoString(this.scriptString);
        }
        catch (Exception e)
        {
            this.enabled = false;
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

    public void StartTimer(string timerName)
    {
        this.times[timerName] = Time.timeAsDouble;
    }

    public void Freeze(string objectName)
    {
        Rigidbody r = this.AllObjects[objectName].GetComponent<Rigidbody>();
        if (r != null)
        {
            r.isKinematic = true;
            r.velocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;
        }
        else
        {
            throw new Exception("Cannot unfreeze static object");
        }
    }

    public void Unfreeze(string objectName)
    {
        Rigidbody r = this.AllObjects[objectName].GetComponent<Rigidbody>();
        if (r != null)
        {
            r.isKinematic = false;
        }
        else
        {
            throw new Exception("Cannot unfreeze static object");
        }
    }

    public double GetTime(string timerName)
    {
        //int milliseconds = (int)(1000f * (Time.time - this.times[timerName]));
        //return (float)(milliseconds) * 0.001f;
        return Time.timeAsDouble - this.times[timerName];
    }

    public static bool Key(char k)
    {
        bool ret = Input.GetKey(InputHelper.chartoKeycode[k]);
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


    public void Print(string s, Table tbl)
    {
        // TODO REDO THIS
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

        var tblt = tbl.Get("t");
        float t = tblt == DynValue.Nil ? 0.1f : (float)tblt.Number;

        DisplaySingleton.Instance.DisplayText(
            txt =>
            {
                DisplaySingleton.BasicMediumModification(txt);
                txt.transform.position = new Vector2(Screen.width * (0.5f + position.x * 0.5f), Screen.height * (0.5f + position.y * 0.5f));
                txt.color = new Color(r, g, b);
                txt.enableWordWrapping = false;
                txt.overflowMode = TMPro.TextOverflowModes.Overflow;
                txt.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
                txt.SetText(s);
            }, t
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

    // SPECIALIZED FUNCTIONS
    // LoadMap places the model at the new map's default spawn before Lua optionally supplies a specific pose.
    public void LoadMap(string mapName)
    {
        string m = mapName;
        if (mapName.Contains(UIStrings.MapExtension))
        {
            m = mapName.Substring(0, mapName.Length - UIStrings.MapExtension.Length);
        }
        if (SingleplayerMenu.myVMap.LoadNewMap(m + UIStrings.MapExtension))
        {
            CoreChip.ClientCoreChip.CmdResetToDefaultLocation();
        }
    }

    public float[] GetPosition(string name)
    {
        Vector3 p = this.AllObjects[name].transform.position;
        return new float[] { p.x, p.y, p.z };
    }

    public void SetRotation(string name, Table tbl)
    {
        Vector3 p = Vector3.zero;
        for (int i = 1; i <= 3; ++i)
        {
            p[i - 1] = float.Parse(tbl[i].ToString());
        }
        if (this.PlayerObjects.Contains(name))
        {
            var core = this.AllObjects[name].GetComponent<CoreChip>();
            core.CmdResetPose(core.transform.position, Quaternion.Euler(p.x, p.y, p.z));
        }
        else
        {
            this.SceneObjects.TryGetValue(name, out GameObject g);
            if (g != null)
            {
                g.transform.rotation = Quaternion.Euler(p.x, p.y, p.z);
            }
            else
            {
                DisplaySingleton.Instance.DisplayText(
                    txt =>
                    {
                        DisplaySingleton.ErrorMsgModification(txt);
                        txt.SetText($"Object {name} doesn't exist!");
                    }, 3f
                );
            }
        }
    }

    public void SetPosition(string name, Table tbl)
    {
        Vector3 p = Vector3.zero;
        for (int i = 1; i <= 3; ++i)
        {
            p[i - 1] = float.Parse(tbl[i].ToString());
        }
        if (this.PlayerObjects.Contains(name))
        {
            var core = this.AllObjects[name].GetComponent<CoreChip>();
            core.CmdResetPose(p, core.transform.rotation);
        }
        else
        {
            this.SceneObjects.TryGetValue(name, out GameObject g);
            if (g != null)
            {
                g.transform.position = p;
            }
            else
            {
                DisplaySingleton.Instance.DisplayText(
                    txt =>
                    {
                        DisplaySingleton.ErrorMsgModification(txt);
                        txt.SetText($"Object {name} doesn't exist!");
                    }, 3f
                );
            }
        }
    }

    public void Hide(string objectName)
    {
        if (this.SceneObjects.TryGetValue(objectName, out GameObject go))
        {
            go.SetActive(false);
        }
    }

    public void Show(string objectName)
    {
        if (this.SceneObjects.TryGetValue(objectName, out GameObject go))
        {
            print($"Showing: {objectName}");
            go.SetActive(true);
        }
    }

    public string Spawn(string objectType, Table properties)
    {
        var _mass = properties.Get("mass");
        var m = (_mass == DynValue.Nil) ? -1f : (float)_mass.Number;
        var _bounciness = properties.Get("bounciness");
        var bounciness = (_bounciness == DynValue.Nil) ? 0f : (float)_bounciness.Number;

        GameObject o;
        switch (objectType.ToLower())
        {
            case "sphere":
                {
                    o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                }
            case "cube":
                {
                    o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                }
            case "capsule":
                {
                    o = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                }
            case "gate":
                {
                    o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject.Destroy(o.GetComponent<Collider>());
                    break;
                }
            case "booster":
                // Booster is also basically a sphere, but we keep its collider
                // (We'll set isTrigger in Booster.Setup)
                o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
            default: { return ""; }
        }
        Vector3 positionVector = GameScript.GetVector3("position", properties);
        Vector3 rotationVector = GameScript.GetVector3("rotation", properties);
        Vector3 sizeVector = GameScript.GetVector3("size", properties);
        Vector4 colourVector = GameScript.GetVector4("color", properties);

        Collider col = o.GetComponent<Collider>();
        if (col != null)
        {
            col.material.bounciness = bounciness;
        }

        MeshRenderer renderer = o.GetComponent<MeshRenderer>();
        Material mat = renderer.material;

        mat.color = new Color(
            colourVector.x < 1f ? colourVector.x : (colourVector.x / 255f),
            colourVector.y < 1f ? colourVector.y : (colourVector.y / 255f),
            colourVector.z < 1f ? colourVector.z : (colourVector.z / 255f),
            colourVector.w < 1f ? colourVector.w : (colourVector.w / 255f)
        );
        mat.SetTransparent();

        o.transform.position = positionVector;
        o.transform.rotation = Quaternion.Euler(rotationVector);
        float radius = 0.5f * Mathf.Max(sizeVector.x, sizeVector.y, sizeVector.z);
        if (objectType.ToLower() == "sphere"
            || objectType.ToLower() == "gate"
            || objectType.ToLower() == "booster")
        {
            o.transform.localScale = Vector3.one * 2f * radius;
        }
        else
        {
            o.transform.localScale = sizeVector;
        }
        // gates are 'fictional' - have no rigid body
        bool isGate = objectType.ToLower() == "gate";
        if (m > 0f && !isGate)
        {
            Rigidbody r;
            if ((r = o.GetComponent<Rigidbody>()) == null)
            {
                r = o.AddComponent<Rigidbody>();
            }
            r.mass = m;
        }
        if (isGate)
        {
            // for gate 1:
            //{options={triggerObjects={sphere1}, radius=2, func=someFunc}
            // for gate 2:
            //{options={triggerObjects={sphere1}, radius=2, func=someFunc}
            // function someFunc1()
            //     scoreboard[1] = scoreboard[1] + 1
            // end
            // function someFunc2()
            //     scoreboard[2] = scoreboard[2] + 1
            // end
            var options = properties.Get("options");
            if (options != DynValue.Nil)
            {
                Table optionsTable = options.Table;
                var triggerObjects = optionsTable.Get("triggerObjects");
                if (options != DynValue.Nil)
                {
                    Table triggerObjectsTable = triggerObjects.Table;
                    GameObject[] os = new GameObject[triggerObjectsTable.Length];
                    for (int i = 0; i < os.Length; ++i)
                    {
                        os[i] = this.AllObjects[triggerObjectsTable[i + 1].ToString()];
                    }
                    var func = optionsTable.Get("func");
                    if (func != DynValue.Nil)
                    {
                        Closure closure = func.Function;
                        Action a = () => closure.Call();
                        Checkpoint chk = o.AddComponent<Checkpoint>();
                        chk.Setup(radius, os, a);
                    }
                }
            }
        }
        if (objectType.ToLower() == "booster")
        {
            // Check if there's an options table
            var options = properties.Get("options");
            Vector3 forceVector = Vector3.zero;
            float customRadius = radius;           // default to the sphere size above

            if (!options.IsNil())
            {
                Table optionsTable = options.Table;

                // 'force' can be a {x=..., y=..., z=...} or {1,2,3} table
                var forceDynVal = optionsTable.Get("force");
                if (!forceDynVal.IsNil())
                {
                    forceVector = GameScript.GetVector3FromTable(forceDynVal.Table);
                }
            }

            // Add the Booster component and set it up
            Booster booster = o.AddComponent<Booster>();
            booster.Setup(forceVector);
        }

        for (int i = 1; ; ++i)
        {
            string k = objectType + i.ToString();
            if (!this.SceneObjects.ContainsKey(k))
            {
                this.SceneObjects[k] = o;
                this.AllObjects[k] = o;
                return k;
            }
        }
    }
    void Update()
    {
        if (this.script == null) { return; }
        this.CheckAttemptValidity();
        this.CallLoop();

        if (this.isRace)
        {
            DynValue res = script.Call(script.Globals["RaceLoop"]);
        }
    }

    public void CallLoop()
    {
        try
        {
            DynValue res = script.Call(script.Globals["Loop"]);
        }
        catch (Exception e)
        {
            DisplaySingleton.Instance.DisplayText(
                x =>
                {
                    DisplaySingleton.ErrorMsgModification(x);
                    x.SetText(e.Message);
                }, 5f
            );
        }
    }

    void OnDestroy()
    {
        this.RestorePhysics();
        foreach (var gkv in SceneObjects)
        {
            // destroy is done asynchronously
            GameObject.Destroy(gkv.Value);
        }
    }

    public static Vector4 GetVector4(string key, Table tbl)
    {
        Vector4 tableVector = Vector4.one * 255f;
        var l = tbl.Get(key);
        if (l != DynValue.Nil)
        {
            Table table = l.Table;
            var _x = table.Get("x");
            if (_x != DynValue.Nil)
            {
                tableVector.x = (float)_x.Number;
            }
            else
            {
                _x = table.Get(1);
                if (_x != DynValue.Nil)
                {
                    tableVector.x = (float)_x.Number;
                }
                else
                {
                    _x = table.Get("r");
                    if (_x != DynValue.Nil)
                    {
                        tableVector.x = (float)_x.Number;
                    }
                }
            }
            var _y = table.Get("y");
            if (_y != DynValue.Nil)
            {
                tableVector.y = (float)_y.Number;
            }
            else
            {
                _y = table.Get(2);
                if (_y != DynValue.Nil)
                {
                    tableVector.y = (float)_y.Number;
                }
                else
                {
                    _y = table.Get("g");
                    if (_y != DynValue.Nil)
                    {
                        tableVector.y = (float)_y.Number;
                    }
                }
            }
            var _z = table.Get("z");
            if (_z != DynValue.Nil)
            {
                tableVector.z = (float)_z.Number;
            }
            else
            {
                _z = table.Get(3);
                if (_z != DynValue.Nil)
                {
                    tableVector.z = (float)_z.Number;
                }
                else
                {
                    _z = table.Get("b");
                    if (_z != DynValue.Nil)
                    {
                        tableVector.z = (float)_z.Number;
                    }
                }
            }
            var _w = table.Get("w");
            if (_w != DynValue.Nil)
            {
                tableVector.w = (float)_w.Number;
            }
            else
            {
                _w = table.Get(4);
                if (_w != DynValue.Nil)
                {
                    tableVector.w = (float)_w.Number;
                }
                else
                {
                    _w = table.Get("a");
                    if (_w != DynValue.Nil)
                    {
                        tableVector.w = (float)_w.Number;
                    }
                }
            }
        }
        return tableVector;
    }

    public static Vector3 GetVector3(string key, Table tbl)
    {
        Vector3 tableVector = Vector3.one;
        var l = tbl.Get(key);
        if (l != DynValue.Nil)
        {
            Table table = l.Table;
            var _x = table.Get("x");
            if (_x != DynValue.Nil)
            {
                tableVector.x = (float)_x.Number;
            }
            else
            {
                _x = table.Get(1);
                if (_x != DynValue.Nil)
                {
                    tableVector.x = (float)_x.Number;
                }
                else
                {
                    _x = table.Get("r");
                    if (_x != DynValue.Nil)
                    {
                        tableVector.x = (float)_x.Number;
                    }
                }
            }
            var _y = table.Get("y");
            if (_y != DynValue.Nil)
            {
                tableVector.y = (float)_y.Number;
            }
            else
            {
                _y = table.Get(2);
                if (_y != DynValue.Nil)
                {
                    tableVector.y = (float)_y.Number;
                }
                else
                {
                    _y = table.Get("g");
                    if (_y != DynValue.Nil)
                    {
                        tableVector.y = (float)_y.Number;
                    }
                }
            }
            var _z = table.Get("z");
            if (_z != DynValue.Nil)
            {
                tableVector.z = (float)_z.Number;
            }
            else
            {
                _z = table.Get(3);
                if (_z != DynValue.Nil)
                {
                    tableVector.z = (float)_z.Number;
                }
                else
                {
                    _z = table.Get("b");
                    if (_z != DynValue.Nil)
                    {
                        tableVector.z = (float)_z.Number;
                    }
                }
            }
        }
        return tableVector;
    }

    public static Vector3 GetVector3FromTable(Table table)
    {
        Vector3 v = Vector3.zero;
        var xVal = table.Get("x"); if (!xVal.IsNil()) v.x = (float)xVal.Number; else if (!table.Get(1).IsNil()) v.x = (float)table.Get(1).Number;
        var yVal = table.Get("y"); if (!yVal.IsNil()) v.y = (float)yVal.Number; else if (!table.Get(2).IsNil()) v.y = (float)table.Get(2).Number;
        var zVal = table.Get("z"); if (!zVal.IsNil()) v.z = (float)zVal.Number; else if (!table.Get(3).IsNil()) v.z = (float)table.Get(3).Number;
        return v;
    }
}

public class Booster : MonoBehaviour
{
    private Vector3 force;
    bool isCustomForce = false;

    public void Setup(Vector3 force)
    {
        this.force = force;
        this.isCustomForce = force != Vector3.zero;

        // Ensure we have a SphereCollider
        var sc = GetComponent<SphereCollider>();
        if (sc == null)
        {
            sc = gameObject.AddComponent<SphereCollider>();
        }
        sc.isTrigger = true;
        this.gameObject.layer = 6;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            if (this.isCustomForce)
            {
                other.attachedRigidbody.AddForce(this.force, ForceMode.Force);
            }
            else
            {
                // Apply force continuously as long as the object stays in the trigger
                other.attachedRigidbody.AddForce(other.attachedRigidbody.velocity.normalized * 100, ForceMode.Force);
            }
        }
    }
}


