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

    public static TValue AccessLikeDict<TKey, TValue>(TKey key, TKey[] keys, TValue[] vals)
    {
        int index = Array.IndexOf(keys, key);
        if(index == -1)
        {
            return default(TValue);
        }
        return vals[index];
    }
}

