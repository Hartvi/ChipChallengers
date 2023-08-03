using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeclaredProp : TopProp
{
    protected virtual void Awake()
    {
        this.Setup();
        //print($"rprop: {this} vprop: {this.vProp}");
        this.AddChildren();
    }
}
