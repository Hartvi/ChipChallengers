using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MoonSharp.Interpreter;
public enum CProp
{
    Angle, Value, Colour, Spring, Damper, Option, Name, Type
}
public enum Orientation
{
    North, West, South, East
}
//public enum CType {
//    Chip, Rudder, Axle, Telescope, Fan, Wheel, Balloon, Cowl
//}

public class VirtualChip
{
    public const string typeStr = "Type";
    public const string angleStr = "Angle";
    public const string springStr = "Spring";
    public const string damperStr = "Damper";

    public const string coreStr = "Core";
    public const string cowlStr = "Cowl";

    public const string chipsFolderStr = "Chips/";
    [NonSerialized]
    public static readonly Dictionary<string, Type> propertyTypes;
    [NonSerialized]
    public static readonly string[] allPropertiesStr = new string[] { "Angle", "Value", "Colour", "Spring", "Damper", "Option", "Name", "Type" };
    [NonSerialized]
    public static readonly string[] dynamicPropertiesStr = new string[] { "Angle", "Value", "Colour" };
    [NonSerialized]
    public static readonly string[] staticPropertiesStr = new string[] { "Spring", "Damper", "Option", "Name", "Type" };
    [NonSerialized]
    public static readonly CProp[] staticPropertiesEnum = new CProp[] { CProp.Spring, CProp.Damper, CProp.Option, CProp.Name, CProp.Type };
    [NonSerialized]
    public static readonly Dictionary<string, CProp> str2ChipProperty;
    [NonSerialized]
    public static readonly string[] chipNames = new string[] { "Chip", "Rudder", "Axle", "Telescope", "Wheel", "Fan", "Sensor", "Cowl" };
    [NonSerialized]
    public static readonly Dictionary<string, CommonChip> chipTemplates;

    public bool IsCore { get { return this.ChipType == VirtualChip.coreStr; } }
    public string ChipType { get { return instanceProperties[typeStr] as string; } }

    static VirtualChip()
    {
        propertyTypes = new Dictionary<string, Type>() {
            {"Angle",  typeof(string)},
            {"Value",  typeof(string)},
            {"Colour", typeof(string)},
            {"Spring", typeof(float)},
            {"Damper", typeof(float)},
            {"Option", typeof(uint)},
            {"Name",   typeof(string)},
            {"Type",   typeof(string)},
        };

        str2ChipProperty = new Dictionary<string, CProp>() {
            {"Angle",  CProp.Angle},
            {"Value",  CProp.Value},
            {"Colour", CProp.Colour},
            {"Spring", CProp.Spring},
            {"Damper", CProp.Damper},
            {"Option", CProp.Option},
            {"Name",   CProp.Name},
            {"Type",   CProp.Type},
        };

        var tmpDict = new Dictionary<string, CommonChip>();
        foreach (var chipName in chipNames)
        {
            var resourceChipName = chipsFolderStr + chipName;
            try
            {
                var resourceObject = Resources.Load<GameObject>(resourceChipName).GetComponent<CommonChip>();

                tmpDict.Add(chipName, resourceObject);
            }
            catch
            {
                throw new ArgumentNullException($"Couldn't load {resourceChipName} chip. Mayhaps it doesn't exist in the Resources folder?");
            }
        }
        chipTemplates = tmpDict;
    }

    // INSTANCE VARIABLES
    public string[] keys;
    public string[] vals;
    public string id = null;
    // CONNECTION DATA
    public int orientation = 0;
    public string parentId = null;

    [NonSerialized]
    public object[] objectVals;
    [NonSerialized] // TODO: link this from the text saved version
    public VirtualChip parentChip;
    [NonSerialized]
    public List<VirtualChip> children = new List<VirtualChip>();
    [NonSerialized]
    public Dictionary<string, object> instanceProperties;

    //[Obsolete("Remove this in the future and load only through editor.")]
    public VirtualChip(string[] keys, string[] vals, int orientation, VirtualChip parentChip)
    {
        this.orientation = orientation;
        this.keys = keys;
        this.vals = vals;
        this.parentChip = parentChip;
        CheckAndSetVals();

        if (parentChip != null)
        {
            this.id = parentChip.GetNewChildID(this);
            this.parentId = parentChip.id;
        }
        else
        {
            this.id = "a";
        }
        //PRINT.print(id);
    }

    public bool TryGetProperty<T>(string key, out T val)
    {
        try
        {
            val = (T)(this.instanceProperties[key]);
            return true;
        }
        catch
        {
            val = default(T);
            return false;
        }
    }

    public T GetProperty<T>(string key)
    {
        return (T)(this.instanceProperties[key]);
    }

    public string GetNewChildID(VirtualChip childChip)
    {
        if (this.id == null)
        {
            throw new FieldAccessException($"VirtualChip's id is null.");
        }

        if (this.children.Count == 0)
        {
            this.children.Add(childChip);
            return id + "a";
        }
        else
        {
            var lastSibling = this.children.Last();
            this.children.Add(childChip);

            var olderSiblingId = lastSibling.id;
            return olderSiblingId.Substring(0, olderSiblingId.Length - 1) + (char)(olderSiblingId.Last() + 1);
        }
    }

    //public string GetChipType() {
    //    return instanceProperties[typeStr] as string;
    //}

    //public bool IsCore() {
    //    return this.GetChipType() == VirtualChip.coreStr;
    //}

    public void CheckAndSetVals()
    {
        if (!this.keys.Contains(VirtualChip.typeStr))
        {
            throw new ArgumentException($"VirtualChip doesn't contain {VirtualChip.typeStr} field.");
        }
        if (this.orientation < 0 || this.orientation > 3)
        {
            throw new IndexOutOfRangeException($"Orientation w.r.t. Parent can only be in [0, 3]; currently attempting to set to {this.orientation}");
        }
        if (this.keys.Length != this.vals.Length)
        {
            throw new ArgumentException("Keys and Values arrays should have the same length.");
        }
        if (this.keys.Length == 0 || this.vals.Length == 0)
        {
            throw new FieldAccessException("Keys or Values arrays are empty.");
        }

        this.objectVals = new object[this.vals.Length];

        for (int i = 0; i < this.keys.Length; i++)
        {
            if (this.keys[i] == "Name")
            {
                if (!StringHelpers.IsVariableName(this.vals[i]))
                {
                    throw new ArgumentException($"Chip name is not a variable name: {this.vals[i]}");
                }
            }
            if (!VirtualChip.propertyTypes.ContainsKey(this.keys[i]))
            {
                throw new ArgumentException($"Unknown key: {this.keys[i]}");
            }

            Type expectedType = VirtualChip.propertyTypes[this.keys[i]];

            if (expectedType == typeof(float))
            {
                if (!float.TryParse(this.vals[i], out float result))
                {
                    //PRINT.print($"'{keys[i]}': '{vals[i]}' is a string, perhaps variable");
                    throw new ArgumentException($"Value {this.vals[i]} at index {i} cannot be converted to float.");
                }
                else
                {
                    this.objectVals[i] = result;
                }
            }
            else if (expectedType == typeof(string))
            { // String does not require conversion
                this.objectVals[i] = this.vals[i];
            }
            else if (expectedType == typeof(uint))
            {
                if (!uint.TryParse(this.vals[i], out uint result))
                {
                    throw new ArgumentException($"Value at index {i} cannot be converted to uint.");
                }
                this.objectVals[i] = result;
            }
            else
            {
                throw new NotSupportedException($"Conversion not supported for type: {expectedType}");
            }
        }
        this.instanceProperties = keys.Zip(this.objectVals, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
    }

    // Returns a string representing this VirtualChip as Lua code
    public string ToLuaString()
    {
        // Initialize string with beginning of table
        string luaCode = "{";

        // Add fixed fields
        if (this.id != null)
        {
            luaCode += $"id = '{this.id}', ";
        }
        luaCode += $"orientation = '{this.orientation}', parentId = '{this.parentId}', ";

        // Add keys and values from arrays
        for (int i = 0; i < this.keys.Length; i++)
        {
            luaCode += $"{this.keys[i]} = '{this.vals[i]}', ";
        }

        // Close table and return
        luaCode += "}";
        return luaCode;
    }

    public static string ArrayToLuaString(VirtualChip[] chips)
    {
        string luaCode = "{";

        foreach (VirtualChip chip in chips)
        {
            luaCode += chip.ToLuaString() + ", ";
        }

        luaCode += "}";
        return luaCode;
    }

    public VirtualChip(Table luaTable)
    {
        this.id = (string)luaTable["id"];
        this.orientation = int.Parse((string)luaTable["orientation"]);
        this.parentId = (string)luaTable["parentId"];

        var indices = luaTable.Keys.Select((item, index) => new { item, index })
               .Where(x => VirtualChip.allPropertiesStr.Contains(x.item.String))
               .Select(x => x.index)
               .ToList();

        var keysList = luaTable.Keys;
        var valsList = luaTable.Values;
        this.keys = keysList.Where((item, index) => indices.Contains(index)).Select(x=>x.String).ToArray();
        this.vals = valsList.Where((item, index) => indices.Contains(index)).Select(x=>x.String).ToArray();
        this.CheckAndSetVals();
    }

    public static VirtualChip[] FromLuaTables(Table[] luaTables)
    {
        return luaTables.Select(t => new VirtualChip(t)).ToArray();
    }
}
