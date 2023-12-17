using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface InputReceiver
{
    protected bool IsActive();

    public void CoreHandleInputs()
    {
        //PRINT.IPrint($"InputReceiver: running");
        if (!this.IsActive())
        {
            Debug.LogWarning($"InputReceiver: {this.GetType()}: Cannot handle inputs on inactive UI object.");
            //throw new InvalidOperationException($"InputReceiver: {this.GetType()}: Cannot handle inputs on inactive UI object.");
        }
        this.HandleInputs();
    }

    protected void HandleInputs();
}

