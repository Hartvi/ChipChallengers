using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface InputReceiver
{
    public virtual void HandleInputs()
    {
        PRINT.IPrint($"InputReceiver: running");
    }
}
