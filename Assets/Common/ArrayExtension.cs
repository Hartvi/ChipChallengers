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
}

