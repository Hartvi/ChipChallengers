using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeCode
{
    public const int defaultLimit = 10000;
    private int currentValue = 0;
    private int currentLimit;

    public SafeCode() {
        currentValue = 0;
        currentLimit = defaultLimit;
    }

    public bool Safe() {
        return currentValue++ < currentLimit;
    }

}
