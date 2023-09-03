using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MoonSharp.Interpreter;

public enum CPR
{
    Angle, Value, Colour, Spring, Damper, Option, Name, Type
}
public enum Orientation
{
    North, West, South, East
}
public enum CTP
{
    Chip, Rudder, Axle, Telescope, Wheel, Fan, Sensor, Cowl
}

public class VChip
{
    public const string nameStr = "Name";
    public const string typeStr = "Type";
    public const string angleStr = "Angle";
    public const string colourStr = "Colour";
    public const string valueStr = "Value";
    public const string springStr = "Spring";
    public const string damperStr = "Damper";
    public const string optionStr = "Option";

    public const string coreStr = "Core";
    public const string cowlStr = "Cowl";

    public const string chipsFolderStr = "Chips/";

    public static readonly Dictionary<string, Type> propertyTypes;
    public static readonly string[] allPropertiesStr = new string[] { "Angle", "Value", "Colour", "Spring", "Damper", "Option", "Name", "Type" };
    public static readonly string[] allPropertiesDefaults = new string[] { "0", "0", "#000000", "0", "0", "0", "chip_name", "Chip" };
    public static readonly string[] dynamicPropertiesStr = new string[] { "Angle", "Value", "Colour" };
    public static readonly string[] staticPropertiesStr = new string[] { "Spring", "Damper", "Option", "Name", "Type" };
    public static readonly CPR[] staticPropertiesEnum = new CPR[] { CPR.Spring, CPR.Damper, CPR.Option, CPR.Name, CPR.Type };

    public static readonly string[] chipNames = new string[] { "Chip", "Rudder", "Axle", "Telescope", "Wheel", "Fan", "Sensor", "Cowl" };
    public static readonly CTP[] chipEnums = new CTP[] { CTP.Chip, CTP.Rudder, CTP.Axle, CTP.Telescope, CTP.Wheel, CTP.Fan, CTP.Sensor, CTP.Cowl };

    public static readonly Dictionary<string, CPR> str2ChipProperty;
    public static readonly Dictionary<string, CommonChip> chipTemplates;
    public static readonly ChipData chipData, optionNames;

    public static readonly Dictionary<CTP, string> chipEnumToName;

    public bool IsCore { get { return this.ChipType == VChip.coreStr; } }
    public string ChipType { get { return instanceProperties[typeStr] as string; } }

    static VChip()
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

        str2ChipProperty = new Dictionary<string, CPR>() {
            {"Angle",  CPR.Angle},
            {"Value",  CPR.Value},
            {"Colour", CPR.Colour},
            {"Spring", CPR.Spring},
            {"Damper", CPR.Damper},
            {"Option", CPR.Option},
            {"Name",   CPR.Name},
            {"Type",   CPR.Type},
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

        chipEnumToName = ArrayExtensions.ToDictionaryFromArrays(chipEnums, chipNames);

        // chip_names[], chip_properties[][]
        VChip.chipData = LUALoader.LoadChipSpecification("chips.lua");
        // chip_names[], option_names[][]
        VChip.optionNames = LUALoader.LoadChipSpecification("optionNames.lua");

        //PRINT.print(VChip.chipToPropertyDict.Keys);
        //PRINT.print(VChip.chipToPropertyDict.Values);

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
    public VChip parentChip;
    [NonSerialized]
    public List<VChip> children = new List<VChip>();
    [NonSerialized]
    public Dictionary<string, object> instanceProperties;
    [NonSerialized]
    public CommonChip rChip;

    //[Obsolete("Remove this in the future and load only through editor.")]
    public VChip(string[] keys, string[] vals, int orientation, VChip parentChip)
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

    public VChip(string chipType, LocalDirection localDirection, VChip parent)
    {
        string[] keys = new string[] { VChip.typeStr };
        string[] vals = new string[] { chipType };

        this.orientation = (int)localDirection;
        this.parentChip = parent;
        this.keys = keys;
        this.vals = vals;

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
        string val = ArrayExtensions.AccessLikeDict(key, this.keys, this.vals);

        PRINT.print($"type: {typeof(T)}, float: {typeof(float)}, are equal: {typeof(T) == typeof(float)}");
        if (!this.instanceProperties.ContainsKey(key) && typeof(T) == typeof(float))
        {
            this.instanceProperties[key] = float.Parse(val);
        }
        if (!this.instanceProperties.ContainsKey(key) && typeof(T) == typeof(int))
        {
            this.instanceProperties[key] = int.Parse(val);
        }
        if (!this.instanceProperties.ContainsKey(key) && typeof(T) == typeof(string))
        {
            this.instanceProperties[key] = val;
        }

        PRINT.print($"key: {key}");
        PRINT.print($"val: {this.instanceProperties[key]}");
        PRINT.print($"type of val: {this.instanceProperties[key].GetType()}");
        PRINT.print($"requested type: {typeof(T)}");
        return (T)(this.instanceProperties[key]);

    }

    public string GetNewChildID(VChip childChip)
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


    public void CheckAndSetVals()
    {
        if (!this.keys.Contains(VChip.typeStr))
        {
            throw new ArgumentException($"VirtualChip doesn't contain {VChip.typeStr} field.");
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
            if (!VChip.propertyTypes.ContainsKey(this.keys[i]))
            {
                throw new ArgumentException($"Unknown key: {this.keys[i]}");
            }

            Type expectedType = VChip.propertyTypes[this.keys[i]];

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

    public string CheckValidityOfPropertyForThisChip(string property, string value)
    {
        if(property == VChip.optionStr)
        {
            string[] options = this.GetOptions();
            uint option;
            if (uint.TryParse(value, out option))
            {
                if (option >= options.Length)
                {
                    return UIStrings.OptionTooHigh(value);
                }
            }
        }
        return this.CheckValidityOfProperty(property, value);
    }

    string CheckValidityOfProperty(string property, string value)
    {

        if (this.HasProperty(property))
        {
            return VChip.PropertyFormatMessage(property, value);
        }
        else
        {
            throw new ArgumentException($"Chip type {this.ChipType} doesn't have property: {property}.");
        }
    }

    static string PropertyFormatMessage(string property, string value)
    {
        switch (property)
        {
            case VChip.nameStr:
                if (!StringHelpers.IsVariableName(value))
                {
                    return UIStrings.NotAVariableMsg(value);
                }
                break;
            case VChip.colourStr:
                if (StringHelpers.IsVariableName(value))
                {
                    return null;
                }
                if (StringHelpers.IsColourString(value))
                {
                    return null;
                }

                if (!StringHelpers.IsVariableName(value))
                {
                    return UIStrings.NotAVariableMsg(value);
                }
                else if (!StringHelpers.IsColourString(value))
                {
                    return UIStrings.NotAColour(value);
                }
                break;
            case VChip.angleStr:
            case VChip.valueStr:
                if (StringHelpers.IsVariableName(value))
                {
                    return null;
                }
                if (StringHelpers.IsFloat(value))
                {
                    return null;
                }

                if (!StringHelpers.IsVariableName(value))
                {
                    return UIStrings.NotAVariableMsg(value);
                }
                else if (!StringHelpers.IsFloat(value))
                {
                    return UIStrings.NotAFloat(value);
                }
                break;
            case VChip.springStr:
            case VChip.damperStr:
                if (!StringHelpers.IsFloat(value))
                {
                    return UIStrings.NotAFloat(value);
                }
                break;
            case VChip.typeStr:
                if (!VChip.chipNames.Contains(value))
                {
                    return UIStrings.NotAType(value);
                }
                break;
            case VChip.optionStr:
                if (!StringHelpers.IsUInt(value))
                {
                    return UIStrings.NotAUInt(value);
                }
                break;
            default:
                throw new ArgumentException($"Unknown chip property {property}.");
        }
        return null;
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

    public static string ArrayToLuaString(VChip[] chips)
    {
        string luaCode = "{";

        foreach (VChip chip in chips)
        {
            luaCode += chip.ToLuaString() + ", ";
        }

        luaCode += "}";
        return luaCode;
    }

    public VChip(Table luaTable)
    {
        this.id = (string)luaTable["id"];
        this.orientation = int.Parse((string)luaTable["orientation"]);
        this.parentId = (string)luaTable["parentId"];

        var indices = luaTable.Keys.Select((item, index) => new { item, index })
               .Where(x => VChip.allPropertiesStr.Contains(x.item.String))
               .Select(x => x.index)
               .ToList();

        var keysList = luaTable.Keys;
        var valsList = luaTable.Values;
        this.keys = keysList.Where((item, index) => indices.Contains(index)).Select(x => x.String).ToArray();
        this.vals = valsList.Where((item, index) => indices.Contains(index)).Select(x => x.String).ToArray();
        this.CheckAndSetVals();
        // TODO trigger model changed callback when variables and chips change
    }

    public static VChip[] FromLuaTables(Table[] luaTables)
    {
        return luaTables.Select(t => new VChip(t)).ToArray();
    }

    public bool HasProperty(string property)
    {
        return ArrayExtensions.AccessLikeDict(this.ChipType, VChip.chipData.keys, VChip.chipData.values).Contains(property);
    }

    public string[] GetOptions() {
        return ArrayExtensions.AccessLikeDict(this.ChipType, VChip.optionNames.keys, VChip.optionNames.values);
    }
}

