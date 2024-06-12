using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class GameScript : MonoBehaviour
{
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


    public static void Print(string s, Table tbl)
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

    // SPECIALIZED FUNCTIONS
    public static void LoadMap(string mapName)
    {
        string m = mapName;
        if (mapName.Contains(UIStrings.MapExtension))
        {
            m = mapName.Substring(0, mapName.Length - UIStrings.MapExtension.Length);
        }
        SingleplayerMenu.myVMap.LoadNewMap(m + UIStrings.MapExtension);
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
            var core = this.AllObjects[name].GetComponent<CommonChip>();
            core.transform.position = p;
            core.transform.rotation = Quaternion.identity;
            core.rb.velocity = Vector3.zero;
            core.TriggerSpawn(core.VirtualModel, false);
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

    //public string AddGateFunction(string objectType, Table properties)
    //{

    //}

    public string Spawn(string objectType, Table properties)
    {
        var _mass = properties.Get("mass");
        var m = (_mass == DynValue.Nil) ? 1f : (float)_mass.Number;
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
            default: { return ""; }
        }
        Vector3 positionVector = GameScript.GetVector3("position", properties);
        Vector3 rotationVector = GameScript.GetVector3("rotation", properties);
        Vector3 sizeVector = GameScript.GetVector3("size", properties);
        Vector4 colourVector = GameScript.GetVector4("color", properties);

        MeshRenderer renderer = o.GetComponent<MeshRenderer>();
        Material mat = renderer.material;
        mat.color = new Color(colourVector.x / 255f, colourVector.y / 255f, colourVector.z / 255f, colourVector.w / 255f);
        mat.SetTransparent();

        o.transform.position = positionVector;
        o.transform.rotation = Quaternion.Euler(rotationVector);
        float radius = 0.5f * Mathf.Max(sizeVector.x, sizeVector.y, sizeVector.z);
        if (objectType.ToLower() == "sphere" || objectType.ToLower() == "gate")
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
        for (int i = 0; ; ++i)
        {
            string k = objectType + i.ToString();
            if (!this.SceneObjects.ContainsKey(k))
            {
                this.SceneObjects[k] = o;
                return k;
            }
        }
    }

    Script script;
    string scriptString;

    Dictionary<string, GameObject> SceneObjects = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> AllObjects = new Dictionary<string, GameObject>();
    HashSet<string> PlayerObjects = new HashSet<string>();

    public void Activate(string scriptString)
    {
        // add owner player
        string player1 = UIStrings.Player + "1";
        AllObjects.Add(player1, CommonChip.ClientCore.gameObject);
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
        this.script.Globals["Spawn"] = (Func<string, Table, string>)Spawn;

        // place holder, user can rewrite it later
        this.script.Globals["Loop"] = (Action)(() => { });

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

}


