using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AngleChip : GeometricChip
{
    protected ConfigurableJoint cj;
    protected Quaternion targetRotation = Quaternion.identity;

    protected Color GetColour()
    {
        string colStr = ArrayExtensions.AccessLikeDict(VChip.colourStr, this.equivalentVirtualChip.keys, this.equivalentVirtualChip.vals);
        if(colStr is null)
        {
            return Color.white;
        }
        if (ColorUtility.TryParseHtmlString(colStr, out Color col)) {
            return col;
        } 
        else
        {
            print("Keys:");
            PRINT.print(this.equivalentVirtualChip.keys);
            print($"colour: {colStr}");
            throw new NotImplementedException($"TODO: implement colours as variables and integers");
        }
    }

    protected float GetAngle() {
        // TODO: make it variable-compatible
        //float angle = (float)(this.equivalentVirtualChip.instanceProperties[VirtualChip.angleStr]);
        string angleStr;
        //if (!this.equivalentVirtualChip.TryGetProperty<string>(VChip.angleStr, out angleStr)) {
        //    return 0f;
        //}
        angleStr = ArrayExtensions.AccessLikeDict(VChip.angleStr, this.equivalentVirtualChip.keys, this.equivalentVirtualChip.vals);
        
        if (angleStr is null) return 0f;

        //print($"anglestr: {angleStr}");
        
        float angle;
        
        if(float.TryParse(angleStr, out angle))
        {
            return angle;
        }

        //print($"angle is {angleStr}");
        if (StringHelpers.IsVariableName(angleStr)) {
            // TODO: do variable angles
            CommonChip core = CommonChip.ClientCore;
            VVar existingVar = core.VirtualModel.variables.FirstOrDefault(x => x.name == angleStr);

            if (existingVar == null)
            {
                if(StringHelpers.IsVariableName(angleStr))
                {
                    core.VirtualModel.AddVariable(VVar.DefaultValueVariable(angleStr));
                    DisplaySingleton.Instance.DisplayText(x =>
                    {
                        DisplaySingleton.WarnMsgModification(x);
                        x.SetText($"Added new variable '{angleStr}'.");
                    }, 3f);
                }
                else
                {
                    DisplaySingleton.Instance.DisplayText(x =>
                    {
                        DisplaySingleton.ErrorMsgModification(x);
                        x.SetText($"Variable name '{angleStr}' is invalid.");
                    }, 3f);
                }
            }
            else
            {
                angle = existingVar.currentValue;
                existingVar.AddValueChangedCallback(x => this.SetAngle(x - existingVar.defaultValue));
            }
        } else {
            // exception here says something is wrong
            angle = float.Parse(angleStr);
        }
        
        return angle;
    }

    public void SetAngle(float a)
    {
        if (this.cj != null)
        {
            //print($"setting angle in function to {a}");
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

}

