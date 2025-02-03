using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T>
{
    public Dictionary<string, int> id2idx = new Dictionary<string, int>();
    public T[] objects;
    public int currentIndex = 0;
    Action<T> deleteObject;

    public ObjectPool(int numObjects, Func<T> genObject, Action<T> deleteObject)
    {
        Debug.Assert(numObjects > 0);
        //PRINT.IPrint($"NUMBER OF OBJECTS: {numObjects}");
        this.objects = new T[numObjects];
        this.deleteObject = deleteObject;

        for (int i = 0; i < numObjects; ++i)
        {
            this.objects[i] = genObject();
        }
    }

    public T Next()
    {
        this.currentIndex = this.currentIndex + 1;
        if (this.currentIndex == objects.Length)
        {
            this.currentIndex = 0;
        }
        return this.objects[this.currentIndex];
    }

    public void DeleteObjects()
    {
        for (int i = 0; i < this.objects.Length; ++i)
        {
            //PRINT.IPrint($"Deleting object {i}: {this.objects[i]}");
            this.deleteObject(this.objects[i]);
        }
    }
}
