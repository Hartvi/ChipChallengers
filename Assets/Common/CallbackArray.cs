using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallbackArray
{
    bool isEditable = false;

    private List<Action> methods = new List<Action>();

    public void AddCallback(Action callback)
    {
        if (!this.isEditable)
        {
            throw new InvalidOperationException($"Cannot add a single listener to a non-editable CallbackArray.");
        }
        this.methods.Add(callback);
    }

    public void SetCallbacks(IEnumerable<Action> callbacks)
    {
        this.methods.Clear();
        this.methods.AddRange(callbacks);
    }

    public void Invoke()
    {
        foreach (var action in this.methods)
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
    bool isEditable = false;

    private List<Action<T>> methods = new List<Action<T>>();

    public void AddCallback(Action<T> callback)
    {
        if (!this.isEditable)
        {
            throw new InvalidOperationException($"Cannot add a single listener to a non-editable CallbackArray.");
        }
        this.methods.Add(callback);
    }

    public void SetCallbacks(IEnumerable<Action<T>> callbacks)
    {
        this.methods.Clear();
        this.methods.AddRange(callbacks);
    }

    public void Invoke(T arg)
    {
        foreach (var action in this.methods)
        {
            action(arg);
        }
    }

    public CallbackArray(bool isEditable)
    {
        this.isEditable = isEditable;
    }

}

