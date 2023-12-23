using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(TestHealth))]
public class TestHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        TestHealth myComponent = (TestHealth)target;
        //myComponent.callfunc();
        if (GUILayout.Button("Spawn Health Box"))
        {
            GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            HealthAspect h = o.AddComponent<HealthAspect>();
            o.AddComponent<Rigidbody>();
            h.SetDeathCallbacks(new Action[] { h.DEBUGDEATH });
            h.SetHealth(100f);
        }
    }
}
#endif
