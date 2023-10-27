using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopScript : MonoBehaviour
{
    public VModel vModel;
    public Action loopFunction;

    public VVar[] variables => this.vModel.variables;

    void Start()
    {
        UnityEngine.Debug.LogWarning($"NOTE: Loopscript only using Update()");
    }
    void Update()
    {
        this.loopFunction();
        // TODO: run this only if variable has been changed this loop
        for (int i = 0; i < this.variables.Length; ++i)
        {
            VVar v = this.variables[i];
            float cur = v.currentValue;
            float diff = v.defaultValue - cur;

            //print($"Default: {v.defaultValue}, current: {cur}");

            if (!v.hasChanged)
            {
                if (Mathf.Abs(diff) > v.backstep)
                {
                    float change = v.backstep * Mathf.Sign(diff);
                    //print($"changing by: {change}");
                    v.currentValue = cur + change;
                } else
                {
                    v.currentValue = v.defaultValue;
                }
            }
            else
            {
                v.hasChanged = false;
            }
        }
    }
}
