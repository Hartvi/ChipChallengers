using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LoopScript : MonoBehaviour
{
    public VModel vModel;
    public Action loopFunction;

    public VVar[] variables => this.vModel.variables;

    [Server]
    public void HandleInputs()
    {
        this.loopFunction();
        VehicleControlClock.Backstep(this.variables);
        // TODO: run this only if variable has been changed this loop
    }

}
