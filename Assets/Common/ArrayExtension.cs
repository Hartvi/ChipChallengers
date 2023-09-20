using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ArrayExtensions
{
    public static T[] AddWithoutDuplicate<T>(this T[] source, T item)
    {
        if (source.Contains(item))
            return source;

        T[] newArray = new T[source.Length + 1];
        Array.Copy(source, newArray, source.Length);
        newArray[source.Length] = item;
        return newArray;
    }
    
    public static T[] RemoveElement<T>(this T[] source, T item)
    {
        return source.Where(e => !e.Equals(item)).ToArray();
    }


    public static Dictionary<TKey, TValue> ToDictionaryFromArrays<TKey, TValue>(TKey[] keys, TValue[] values)
    {
        if (keys == null || values == null)
            throw new ArgumentNullException("Both keys and values arrays must be provided.");

        if (keys.Length != values.Length)
            throw new ArgumentException("Keys and values arrays must be of the same length.");

        return keys.Zip(values, (k, v) => new { Key = k, Value = v })
                   .ToDictionary(x => x.Key, x => x.Value);
    }

    public static TValue AccessLikeDict<TKey, TValue>(TKey key, IList<TKey> keys, IList<TValue> vals)
    {
        int index = keys.IndexOf(key);
        //int index = Array.IndexOf(keys, key);
        if(index == -1)
        {
            return default(TValue);
        }
        return vals[index];
    }

    public static Vector3 Multiply(this Vector3 v, Vector3 v2)
    {
        return new Vector3(v.x * v2.x, v.y * v2.y, v.z * v2.z);
    }

}

public class CustomArray<T> : IList<T>
{
#if UNITY_EDITOR
    static bool hasWarned = false;
#endif
    private T[] _vals;

    public CustomArray(int size)
    {
        _vals = new T[size];
    }

    public CustomArray(T[] initialValues)
    {
        //PRINT.print(initialValues);
        this._vals = initialValues;
        //PRINT.print($"_vals: {_vals.Length}");
    }

    public T this[int index]
    {
        get { return _vals[index]; }
        set
        {
            //PRINT.print($"setting element {index}");
            _vals[index] = value;
            //PRINT.print($"index: {index}, new val: {value}");
            if (this._actions is not null)
            {
                //PRINT.print($"Actions: {this._actions.Length}");
                for (int i = 0; i < this._actions.Length; ++i)
                {
                    this._actions[i]();
                }
            }
        }
    }

    public Action[] _actions;
    public void SetSetListeners(Action[] actions)
    {
        //this._actions = new Action[actions.Length];
        //actions.CopyTo(this._actions, 0);
        //PRINT.print($"Setting listener");
        this._actions = actions;
    }

    public int Count => _vals.Length;
    public int Length => _vals.Length;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        Array.Resize(ref _vals, _vals.Length + 1);
        _vals[_vals.Length - 1] = item;
    }

    public void Clear() => Array.Clear(_vals, 0, _vals.Length);

    public bool Contains(T item) => Array.Exists(_vals, e => e.Equals(item));

    public void CopyTo(T[] array, int arrayIndex) => _vals.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_vals).GetEnumerator();

    public int IndexOf(T item) => Array.IndexOf(_vals, item);

    public void Insert(int index, T item)
    {
        Array.Resize(ref _vals, _vals.Length + 1);
        for (int i = _vals.Length - 1; i > index; i--)
        {
            _vals[i] = _vals[i - 1];
        }
        _vals[index] = item;
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        for (int i = index + 1; i < _vals.Length; i++)
        {
            _vals[i - 1] = _vals[i];
        }
        Array.Resize(ref _vals, _vals.Length - 1);
    }

    IEnumerator IEnumerable.GetEnumerator() => _vals.GetEnumerator();

    // To easily retrieve underlying array
    public T[] ToArray() => _vals;

    public void ReplaceData(T[] data)
    {
        this._vals = data;
    }
    public void ReplaceData(CustomArray<T> data)
    {
        this._vals = (T[])data;
    }

    // Implicit conversion from string[] to CustomArray
    public static implicit operator CustomArray<T>(T[] array)
    {
#if UNITY_EDITOR
        if (!CustomArray<T>.hasWarned)
        {
            CustomArray<T>.hasWarned = true;
            UnityEngine.Debug.LogWarning($"Implicit asignment removes callback listeners. Use ReplaceData instead if you want to keep them.");
        }
#endif
        return new CustomArray<T>(array);
    }


    // Explicit conversion from CustomArray to string[] (optional for convenience)
    public static explicit operator T[](CustomArray<T> customArray)
    {
        return customArray._vals;
    }
}

