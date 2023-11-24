using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTransition: BaseButton
{
    // Any class derived from this must implement a `new public static Function()`

    protected static Dictionary<Type, Action[]> _afterClickedCallbacks = new Dictionary<Type, Action[]>();
    protected Action[] afterClickedCallbacks
    {
        get
        {
            return BaseTransition._afterClickedCallbacks[this.GetType()];
        }
        set
        {
            BaseTransition._afterClickedCallbacks[this.GetType()] = value;
        }
    }

    protected static void SetAfterClickedCallbacks(Type type, Action[] acs)
    {
        BaseTransition._afterClickedCallbacks[type] = acs;
    }

    protected void InvokeAfterClickedCallbacks()
    {
        foreach (Action a in this.afterClickedCallbacks)
        {
            a();
        }
    }

    protected static void InvokeAfterClickedCallbacks(Type type)
    {
        if (!BaseTransition._afterClickedCallbacks.ContainsKey(type)) return;

        foreach (Action a in BaseTransition._afterClickedCallbacks[type])
        {
            a();
        }
    }

    public static void Function(bool switchBack)
    {
        throw new NotImplementedException();
    }

}

