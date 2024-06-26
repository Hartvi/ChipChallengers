using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AngleChip : OptionChip
{
    protected ConfigurableJoint cj;
    protected MeshRenderer[] mrs;
    protected Material[] materials;
    protected Quaternion targetRotation = Quaternion.identity;

    protected CallbackArray RemoveMeFromVariableCallbacks = new CallbackArray(true);

    // these will be listened to during FixedUpdate
    protected float _value = 0f;
    public float value { get { return this._value; } }

    protected float _brake = 0f;
    public float brake { get { return this._brake; } }

    public delegate bool ParseFuncDelegate<T>(string s, out T result);

    //void Update()
    //{
    //    print($"joint: {this.cj.targetRotation}");
    //}

    protected float GetBrake()
    {
        Action<float, VVar> SetBrakeDelegate = (x, v) => this.SetBrake(x);
        
        return this.GetProperty<float>(VChip.brakeStr, float.TryParse, SetBrakeDelegate);
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
                existingVar.AddValueChangedCallback(VariableCallbackFunction);

                // add also the option to remove the callback in case the chip dies
                this.RemoveMeFromVariableCallbacks.AddCallback(() => existingVar.RemoveValueChangedCallback(VariableCallbackFunction));
            }
        }
        else
        {
            // exception here says something is wrong with the whole implementation, SHOULDN't HAPPEN
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
            //print($"setting angle in function to {a}");
            //print($"wuaternoin: {Quaternion.Euler(a, 0f, 0f)}");
            // TODO ITS NOT `this.cj` SINCE THIS IS RUNNING ON THE CORE, IT HAS TO REFERENCE THE TARGET JOINTS
            // maybe, not sure
            //print($"Setting angle on {this.GetInstanceID()}");
            this.cj.targetRotation = Quaternion.Euler(a, 0f, 0f);
        }
        else
        {
            print($"Trying to set angle: {Quaternion.Euler(a, 0f, 0f)}: {a} but joint is NULL, chip: {this.equivalentVirtualChip.id}");
            //throw new NullReferenceException($"Trying to set angle: {Quaternion.Euler(a, 0f, 0f)} but joint is NULL, chip: {this.equivalentVirtualChip.id}");
        }
    }

    [RuntimeFunction]
    public void SetColour(float a)
    {
        if (this.materials != null)
        {
            //print($"Setting colour: {a.ToColor()}");
            foreach (Material m in this.materials)
            {
                m.color = a.ToColor();
            }
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
        //print($"Setting value to {a}");
        this._value = a;
    }

    [RuntimeFunction]
    public void SetBrake(float a)
    {
        // TODO BRAKE FUNCTIONS
        //print($"Setting brake {a}");
        this._brake = Mathf.Abs(a);
    }

}

