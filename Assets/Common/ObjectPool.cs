using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T>
{
    public T[] objects;
    int currentIndex = 0;

    public ObjectPool(int numObjects, Func<T> genObject)
    {
        this.objects = new T[numObjects];

        for (int i = 0; i < numObjects; ++i)
        {
            this.objects[i] = genObject();
        }
    }

    public T Next() {
        this.currentIndex = this.currentIndex + 1;
        if(this.currentIndex == objects.Length)
        {
            this.currentIndex = 0;
        }
        return this.objects[this.currentIndex];
    }
}
