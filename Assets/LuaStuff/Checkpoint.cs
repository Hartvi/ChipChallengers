using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public float radius = 1f;
    float radiusSqr = 1f;
    public GameObject[] targetObjects;
    public Action action;

    public void Setup(float radius, GameObject[] targetObjects, Action action)
    {
        this.radius = radius;
        this.radiusSqr = radius * radius;
        this.targetObjects = targetObjects;
        this.action = action;
    }

    void Update()
    {
        if (targetObjects == null) { return; }
        for (int i = 0; i < targetObjects.Length; ++i)
        {
            var to = targetObjects[i];
            if (to != null)
            {
                if ((to.transform.position - this.transform.position).sqrMagnitude <= radiusSqr)
                {
                    action();
                }
            }
        }
    }
}
