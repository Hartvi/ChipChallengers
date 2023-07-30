using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DeclaredProp : TopProp
{
    protected virtual void Awake()
    {
        this.AddChildren();
    }

}
