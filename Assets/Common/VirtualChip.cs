using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum CProp {
    Angle, Value, Colour, Spring, Damper, Option, Name, Type
}
public enum Orientation {
    North, West, South, East
}
//public enum CType {
//    Chip, Rudder, Axle, Telescope, Fan, Wheel, Balloon, Cowl
//}

public class VirtualChip {
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

    static VirtualChip() {
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
        foreach (var chipName in chipNames) {
            var resourceChipName = chipsFolderStr + chipName;
            try
            {
                var resourceObject = Resources.Load<GameObject>(resourceChipName).GetComponent<CommonChip>();

                tmpDict.Add(chipName, resourceObject);
            } catch
            {
                throw new ArgumentNullException($"Couldn't load {resourceChipName} chip. Mayhaps it doesn't exist in the Resources folder?");
            }
        }
        chipTemplates = tmpDict;
    }

    // INSTANCE VARIABLES
    public string[] keys;
    public string[] vals;
    public object[] objectVals;
    public string id = null;
    // CONNECTION DATA
    public int orientation = 0;

    [NonSerialized] // TODO: link this from the text saved version
    public VirtualChip parentChip;
    [NonSerialized]
    public List<VirtualChip> children = new List<VirtualChip>();
    [NonSerialized]
    public Dictionary<string, object> instanceProperties;

    public VirtualChip(string[] keys, string[] vals, int orientation, VirtualChip parentChip) {
        if(!keys.Contains(typeStr)) {
            throw new ArgumentException($"VirtualChip doesn't contain {typeStr} field.");
        }
        if (orientation < 0 || orientation > 3) {
            throw new IndexOutOfRangeException($"Orienation w.r.t. parent can only be in [0, 3]; currently attempting to set to {orientation}");
        }
        this.orientation = orientation;
        this.keys = keys;
        this.vals = vals;
        CheckAndSetVals();
        this.parentChip = parentChip;
        instanceProperties = keys.Zip(objectVals, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

        if(parentChip != null) {
            id = parentChip.GetNewChildID(this);
        } else {
            id = "a";
        }
        PRINT.print(id);
    }

    public bool TryGetProperty<T>(string key, out T val) {
        try { 
            val = (T)(instanceProperties[key]); 
            return true; 
        } catch { 
            val = default(T); 
            return false; 
        }
    }

    public T GetProperty<T>(string key) {
        return (T)(instanceProperties[key]);
    }

    public string GetNewChildID(VirtualChip childChip) {
        if(id == null) {
            throw new FieldAccessException($"VirtualChip's id is null.");
        }

        if(children.Count == 0) {
            children.Add(childChip);
            return id + "a";
        } else {
            var lastSibling = children.Last();
            children.Add(childChip);

            var olderSiblingId = lastSibling.id;
            return olderSiblingId.Substring(0, olderSiblingId.Length - 1) + (char)(olderSiblingId.Last() + 1);
        }
    }

    public string GetChipType() {
        return instanceProperties[typeStr] as string;
    }

    public void CheckAndSetVals() {
        if (keys.Length != vals.Length) {
            throw new ArgumentException("Keys and Values arrays should have the same length.");
        }
        if (keys.Length == 0 || vals.Length == 0) {
            throw new FieldAccessException("Keys or Values arrays are empty.");
        }

        objectVals = new object[vals.Length];

        for (int i = 0; i < keys.Length; i++) {
            if (keys[i] == "Name") {
                if (!StringHelpers.IsVariableName(vals[i])) {
                    throw new ArgumentException($"Chip name is not a variable name: {vals[i]}");
                }
            }
            if (!propertyTypes.ContainsKey(keys[i])) {
                throw new ArgumentException($"Unknown key: {keys[i]}");
            }

            Type expectedType = propertyTypes[keys[i]];

            if (expectedType == typeof(float)) {
                if (!float.TryParse(vals[i], out float result)) {
                    //PRINT.print($"'{keys[i]}': '{vals[i]}' is a string, perhaps variable");
                    throw new ArgumentException($"Value {vals[i]} at index {i} cannot be converted to float.");
                } else {
                    objectVals[i] = result;
                }
            }
            else if (expectedType == typeof(string)) { // String does not require conversion
                objectVals[i] = vals[i];
            } else if (expectedType == typeof(uint)) {
                if (!uint.TryParse(vals[i], out uint result)) {
                    throw new ArgumentException($"Value at index {i} cannot be converted to uint.");
                }
                objectVals[i] = result;
            } else {
                throw new NotSupportedException($"Conversion not supported for type: {expectedType}");
            }
        }
    }
}
 