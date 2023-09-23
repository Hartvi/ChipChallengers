using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AngleChip : GeometricChip
{
    protected ConfigurableJoint cj;
    protected MeshRenderer mr;
    protected Material material;
    protected Quaternion targetRotation = Quaternion.identity;

    // these will be listened to during FixedUpdate
    //protected float __value;
    protected float _value;
    //{
    //    get { return this.__value; }
    //    set { print($"setting value to {value}"); 
    //        this.__value = value; }
    //}
    public float value { get { return this._value; } }
    protected float _brake;
    public float brake { get; }

    public delegate bool ParseFuncDelegate<T>(string s, out T result);

    protected float GetBrake()
    {
        Action<float, VVar> SetBrakeDelegate = (x, v) => this.SetBrake(x);
        
        return this.GetProperty<float>(VChip.valueStr, float.TryParse, SetBrakeDelegate);
    }

    protected float GetValue()
    {
        Action<float, VVar> SetValueDelegate = (x, v) => this.SetValue(x);
        
        return this.GetProperty<float>(VChip.valueStr, float.TryParse, SetValueDelegate);
    }

    protected Color GetColour()
    {
        Action<float, VVar> SetColourDelegate = (x, v) => this.SetColour(x);
        
        return this.GetProperty<Color>(VChip.colourStr, StringHelpers.ParseColorOrInt, SetColourDelegate);
    }

    // should be equivalent but it's ugly
    //protected float GetAngle() => GetProperty<float>(VChip.angleStr, float.TryParse, (x, v) => this.SetAngle(x - v.defaultValue));
    protected float GetAngle()
    {
        Action<float, VVar> SetAngleDelegate = (x, v) => this.SetAngle(x - v.defaultValue);

        return this.GetProperty<float>(VChip.angleStr, float.TryParse, SetAngleDelegate);
    }

    protected T GetProperty<T>(string propertyName, ParseFuncDelegate<T> ParseFunc, Action<float, VVar> VariableCallbackFunction)
    {

        string propertyStr = ArrayExtensions.AccessLikeDict(propertyName, this.equivalentVirtualChip.keys, this.equivalentVirtualChip.vals);

        if (propertyStr is null)
        {
            propertyStr = ArrayExtensions.AccessLikeDict(propertyName, VChip.allPropertiesStr, VChip.allPropertiesDefaultsStrings);
            if (propertyStr == null)
            {
                UnityEngine.Debug.LogError($"{propertyName}: property str is null even for default values, type: {typeof(T)}");
                return default(T);
            }
        }

        T property;

        if (ParseFunc(propertyStr, out property))
        {
            //print($"property str: {propertyStr}, val: {property}");
            return property;
        }

        if (StringHelpers.IsVariableName(propertyStr))
        {

            CommonChip core = CommonChip.ClientCore;
            VVar existingVar = core.VirtualModel.variables.FirstOrDefault(x => x.name == propertyStr);

            if (existingVar == null)
            {
                //print($"Property: doesnt exist: {propertyName}: {property}");
                if (StringHelpers.IsVariableName(propertyStr))
                {
                    core.VirtualModel.AddVariable(VVar.DefaultValueVariable(propertyStr));
                    DisplaySingleton.Instance.DisplayText(x =>
                    {
                        DisplaySingleton.WarnMsgModification(x);
                        x.SetText($"Added new variable '{propertyStr}'.");
                    }, 3f);
                }
                else
                {
                    DisplaySingleton.Instance.DisplayText(x =>
                    {
                        DisplaySingleton.ErrorMsgModification(x);
                        x.SetText($"Variable name '{propertyStr}' is invalid.");
                    }, 3f);
                }
            }
            else
            {
                property = existingVar.currentValue.FromVariableFloat<T>();
                //print($"Property {propertyName}: exists: {property}");
                existingVar.AddValueChangedCallback(VariableCallbackFunction);
            }
        }
        else
        {
            // exception here says something is wrong in the whole implementation, SHOULDN't HAPPEN
            if (ParseFunc(propertyStr, out property))
            {
                return property;
            }
            else
            {
                throw new ArgumentNullException($"Property {propertyName} cannot be parsed: {propertyStr}, type: {typeof(T)}, chip: {this.equivalentVirtualChip.ChipType}");
            }
        }

        return property;
    }

    [RuntimeFunction]
    public void SetAngle(float a)
    {
        if (this.cj != null)
        {
            print($"setting angle in function to {a}");
            //print($"wuaternoin: {Quaternion.Euler(a, 0f, 0f)}");
            // TODO ITS NOT `this.cj` SINCE THIS IS RUNNING ON THE CORE, IT HAS TO REFERENCE THE TARGET JOINTS
            // maybe, not sure
            this.cj.targetRotation = Quaternion.Euler(a, 0f, 0f);
        }
        else
        {
            //print($"Trying to set angle: {Quaternion.Euler(a, 0f, 0f)} but joint is NULL, chip: {this.equivalentVirtualChip.id}");
            throw new NullReferenceException($"Trying to set angle: {Quaternion.Euler(a, 0f, 0f)} but joint is NULL, chip: {this.equivalentVirtualChip.id}");
        }
    }

    [RuntimeFunction]
    public void SetColour(float a)
    {
        if (this.material != null)
        {
            //print($"Setting colour: {a.ToColor()}");
            this.material.color = a.ToColor();
        }
        else
        {
            throw new NullReferenceException($"Trying to set colour: {a.ToColor()} but material is NULL, chip: {this.equivalentVirtualChip.id}");
        }
    }

    [RuntimeFunction]
    public void SetValue(float a)
    {
        // TODO VALUE FUNCTIONS
        this._value = a;
        //if ()
        //{
        //    // wheel, jet: setvalue => wheel.value = a
        //}
        //else
        //{
        //    throw new NullReferenceException($"Trying to set value: {null} but material is NULL, chip: {this.equivalentVirtualChip.id}");
        //}
    }

    [RuntimeFunction]
    public void SetBrake(float a)
    {
        // TODO BRAKE FUNCTIONS
        this._brake = a;
        //if ()
        //{
        //    // wheel, jet: setvalue => wheel.value = a
        //}
        //else
        //{
        //    throw new NullReferenceException($"Trying to set value: {null} but material is NULL, chip: {this.equivalentVirtualChip.id}");
        //}
    }

}

