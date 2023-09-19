using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopScript : MonoBehaviour
{
    public Action loopFunction;

    void Start()
    {
        UnityEngine.Debug.LogWarning($"NOTE: Loopscript only using Update()");
    }
    void Update()
    {
        this.loopFunction();
    }
}
