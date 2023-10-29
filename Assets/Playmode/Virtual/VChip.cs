using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MoonSharp.Interpreter;

public enum CPR
{
    Angle, Value, Brake, Colour, Spring, Damper, Option, Name, Type
}
public enum Orientation
{
    North, West, South, East
}
public enum CTP
{
    Chip, Rudder, Axle, Gun, Wheel, Jet, Sensor, Cowl
}

public class VChip
{
    public const string nameStr = "Name";
    public const string typeStr = "Type";
    public const string angleStr = "Angle";
    public const string colourStr = "Colour";
    public const string valueStr = "Value";
    public const string brakeStr = "Brake";
    public const string springStr = "Spring";
    public const string damperStr = "Damper";
    public const string optionStr = "Option";

    public const string coreStr = "Core";
    public const string cowlStr = "Cowl";
    public const string sensorStr = "Sensor";
    public const string wheelStr = "Wheel";
    public const string jetStr = "Jet";
    public const string chipStr = "Chip";
    public const string rudderStr = "Rudder";
    public const string axleStr = "Axle";
    public const string gunStr = "Gun";

    public const string chipsFolderStr = "Chips/";

    public static readonly Dictionary<string, Type> propertyTypes;
    public static readonly string[] allPropertiesStr = new string[] { "Angle", "Value", "Brake", "Colour", "Spring", "Damper", "Option", "Name", "Type" };
    public static readonly string[] allPropertiesDefaultsStrings = new string[] { "0", "0", "0", "#FFFFFF", "1e9", "1e6", "0", "chip_name", "Chip" };
    public static readonly object[] allPropertiesDefaultsObjects = new object[] { 0f, 0f, 0f, "#FFFFFF", 1e9f, 1e6f, 0, "chip_name", "Chip" };
    public static readonly string[] dynamicPropertiesStr = new string[] { "Angle", "Value", "Brake", "Colour" };
    public static readonly string[] staticPropertiesStr = new string[] { "Spring", "Damper", "Option", "Name", "Type" };
    public static readonly CPR[] staticPropertiesEnum = new CPR[] { CPR.Spring, CPR.Damper, CPR.Option, CPR.Name, CPR.Type };

    public static readonly string[] chipNames = new string[] { chipStr, rudderStr, axleStr, gunStr, wheelStr, jetStr, sensorStr, cowlStr };

    public static readonly CommonChip baseChip;
    public const string baseChipName = "BaseChip";
    
    public static readonly CTP[] chipEnums = new CTP[] { CTP.Chip, CTP.Rudder, CTP.Axle, CTP.Gun, CTP.Wheel, CTP.Jet, CTP.Sensor, CTP.Cowl };

    public static readonly Dictionary<string, CPR> str2ChipProperty;
    public static readonly Dictionary<string, GameObject> chipTemplates;
    public static readonly ChipData chipData, optionNames;

    public static readonly Dictionary<CTP, string> chipEnumToName;
    public static readonly Dictionary<string, CTP> chipNameToEnum;

    private VModel _MyModel;
    
    public VModel MyModel
    {
        get { return this._MyModel; }
        set
        {
            //PRINT.print($"setting callback to model change trigger");
            this.keys.SetSetListeners(new Action[] { value.TriggerModelChanged });
            this.vals.SetSetListeners(new Action[] { value.TriggerModelChanged });
            this._MyModel = value;
            //value.TriggerModelChanged();
        }
    }

    public bool IsCore { get { return this.ChipType == VChip.coreStr; } }
    public string ChipType
    {
        get
        {
            this.TryGetProperty<string>(VChip.typeStr, out string typeVal);
            return typeVal;
        }
    }

    static VChip()
    {
        propertyTypes = new Dictionary<string, Type>() {
            {VChip.angleStr,  typeof(string)},
            {VChip.valueStr,  typeof(string)},
            {VChip.brakeStr,  typeof(string)},
            {VChip.colourStr, typeof(string)},
            {VChip.springStr, typeof(float)},
            {VChip.damperStr, typeof(float)},
            {VChip.optionStr, typeof(int)},
            {VChip.nameStr,   typeof(string)},
            {VChip.typeStr,   typeof(string)},
        };

        str2ChipProperty = new Dictionary<string, CPR>() {
            {VChip.angleStr,  CPR.Angle},
            {VChip.valueStr,  CPR.Value},
            {VChip.brakeStr,  CPR.Brake},
            {VChip.colourStr, CPR.Colour},
            {VChip.springStr, CPR.Spring},
            {VChip.damperStr, CPR.Damper},
            {VChip.optionStr, CPR.Option},
            {VChip.nameStr,   CPR.Name},
            {VChip.typeStr,   CPR.Type},
        };

        var tmpDict = new Dictionary<string, GameObject>();

        // base chip serves as base for all chips. The other chips are there purely for cosmetic reasons
        VChip.baseChip = Resources.Load<GameObject>(VChip.chipsFolderStr + VChip.baseChipName).GetComponent<CommonChip>();

        foreach (var chipName in VChip.chipNames)
        {
            // "Chips/" + Chip/Rudder/Wheel/etc
            var resourceChipName = VChip.chipsFolderStr + chipName;

            try
            {
                var resourceObject = Resources.Load<GameObject>(resourceChipName);

                tmpDict.Add(chipName, resourceObject);
            }
            catch
            {
                throw new ArgumentNullException($"Couldn't load {resourceChipName} chip. Mayhaps it doesn't exist in the Resources folder?");
            }
        }
        VChip.chipTemplates = tmpDict;

        VChip.chipEnumToName = ArrayExtensions.ToDictionaryFromArrays(VChip.chipEnums, VChip.chipNames);
        VChip.chipNameToEnum = ArrayExtensions.ToDictionaryFromArrays(VChip.chipNames, VChip.chipEnums);

        // chip_names[], chip_properties[][]
        VChip.chipData = LUALoader.LoadChipSpecification("chips.lua");
        // chip_names[], option_names[][]
        VChip.optionNames = LUALoader.LoadChipSpecification("optionNames.lua");

        //PRINT.print(VChip.chipToPropertyDict.Keys);
        //PRINT.print(VChip.chipToPropertyDict.Values);

    }

    // INSTANCE VARIABLES
    private CustomArray<string> _keys;
    public CustomArray<string> keys
    {
        get
        {
            return this._keys;
        }
        set
        {
            if(this._keys is null)
            {
                this._keys = value;
            }
            this._keys.ReplaceData(value);
            //this._keys = value;
            //PRINT.print($"Keys: {this._keys.Length}");
            //PRINT.print($"value: {value}");
            //value.SetSetListeners(new Action[] {});
            //PRINT.print($"model: {MyModel == null}");
            // TODO add this when the model is added, not the keys
        }
    }
    private CustomArray<string> _vals;
    public CustomArray<string> vals
    {
        get
        {
            return this._vals;
        }
        set
        {
            if(this._vals is null)
            {
                this._vals = value;
            }
            this._vals.ReplaceData(value);
        }
    }

    public string id = null;
    // CONNECTION DATA
    public int orientation = 0;
    public string parentId = null;

    [NonSerialized]
    public object[] objectVals;
    [NonSerialized] // TODO: link this from the text saved version
    public VChip parentChip;
    //[NonSerialized]
    //private List<VChip> _childrenList = new List<VChip>();

    public VChip[] Children = { };

    //[NonSerialized]
    //private Dictionary<string, object> instanceProperties;
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

    //public T GetPropertyOrDefault<T>(string key)
    //{
    //    if (this.instanceProperties.ContainsKey(key))
    //    {
    //        T ret = (T)(this.instanceProperties[key]);
    //        if(key == VChip.springStr || key == VChip.damperStr)
    //        {
    //            if(((float)this.instanceProperties[key]) >= 0f){
    //                return ret;
    //            }
    //        } else if(key == VChip.optionStr)
    //        {
    //            if(((int)this.instanceProperties[key]) >= 0){
    //                return ret;
    //            }
    //        }
    //    }
    //    //PRINT.print($"Contains key: {key} FALSE");
    //    //PRINT.print("this.keys:");
    //    //PRINT.print(this.keys);
    //    object objVal = ArrayExtensions.AccessLikeDict(key, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);
    //    return (T)objVal;
    //}

    public bool TryGetProperty<T>(string key, out T val)
    {
        try
        {
            // TODO: rewrite instanceproperties when a field is changed
            string strVal = ArrayExtensions.AccessLikeDict(key, this.keys, this.vals);
            if (propertyTypes[key] == typeof(float))
            {
                val = (T)(object)float.Parse(strVal);
            }
            else if (propertyTypes[key] == typeof(int))
            {
                val = (T)(object)int.Parse(strVal);
            }
            else
            {
                val = (T)(object)strVal;
            }
            //val = (T)(this.instanceProperties[key]);
            //PRINT.IPrint($"val: {val}, chip: {this.ChipType}");
            return true;
        }
        catch
        {

            object objVal = ArrayExtensions.AccessLikeDict(key, VChip.allPropertiesStr, VChip.allPropertiesDefaultsObjects);
            //PRINT.IPrint($"objval: {objVal} type: {objVal.GetType()}");
            val = (T)objVal;
            //val = default(T);
            return false;
        }
    }

    public string GetNewChildID(VChip childChip)
    {
        if (this.id == null)
        {
            throw new FieldAccessException($"VirtualChip's id is null.");
        }

        if (this.Children.Length == 0)
        {
            this.Children = this.Children.Append(childChip).ToArray();
            return id + "a";
        }
        else
        {
            int indexOfLargestSibling = StringHelpers.GetIndexOfLargest(this.Children.Select(x => x.id).ToArray());
            
            VChip lastSibling = this.Children[indexOfLargestSibling];

            this.Children = this.Children.Append(childChip).ToArray();

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
                    PRINT.IPrint($"'{keys[i]}': '{vals[i]}' is a string, perhaps variable");
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
            else if (expectedType == typeof(int))
            {
                if (!int.TryParse(this.vals[i], out int result))
                {
                    throw new ArgumentException($"Value at index {i} cannot be converted to int.");
                }
                this.objectVals[i] = result;
            }
            else
            {
                throw new NotSupportedException($"Conversion not supported for type: {expectedType}");
            }
        }
        //this.instanceProperties = keys.Zip(this.objectVals, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
    }

    public string CheckValidityOfPropertyForThisChip(string property, string value)
    {
        if(property == VChip.optionStr)
        {
            string[] options = this.GetOptions();
            int option;
            if (int.TryParse(value, out option))
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
            case VChip.brakeStr:
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

    public (string[] keys, string[] values, int orientation, VChip parent) GetValueTuple()
    {
        string[] keysCopy = new string[this.keys.Length];
        string[] valsCopy = new string[this.vals.Length];

        Array.Copy((string[])this.keys, keysCopy, this.keys.Length);
        Array.Copy((string[])this.vals, valsCopy, this.vals.Length);

        return (keysCopy, valsCopy, this.orientation, this.parentChip);
    }

    public bool HasHealth()
    {
        return this.ChipType != VChip.cowlStr;
    }

    public float DefaultHealth()
    {
        Debug.LogWarning($"TODO: set reasonable default health values");
        return 1f;
    }
}
