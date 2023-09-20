using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallbackArray
{
    bool isEditable;

    private List<Action> methods = new List<Action>();

    public void AddCallback(Action callback)
    {
        if (!this.isEditable)
        {
            throw new InvalidOperationException($"Cannot add a single listener to a non-editable CallbackArray.");
        }
        methods.Add(callback);
    }

    public void SetCallbacks(IEnumerable<Action> callbacks)
    {
        methods.Clear();
        methods.AddRange(callbacks);
    }

    public void Invoke()
    {
        foreach (var action in methods)
        {
            action();
        }
    }

    public CallbackArray(bool isEditable)
    {
        this.isEditable = isEditable;
    }

}

public class CallbackArray<T>
{
    bool isEditable;

    private List<Action<T>> methods = new List<Action<T>>();

    public void AddCallback(Action<T> callback)
    {
        if (!this.isEditable)
        {
            throw new InvalidOperationException($"Cannot add a single listener to a non-editable CallbackArray.");
        }
        methods.Add(callback);
    }

    public void SetCallbacks(IEnumerable<Action<T>> callbacks)
    {
        methods.Clear();
        methods.AddRange(callbacks);
    }

    public void Invoke(T arg)
    {
        foreach (var action in methods)
        {
            action(arg);
        }
    }

    public CallbackArray(bool isEditable)
    {
        this.isEditable = isEditable;
    }

}

