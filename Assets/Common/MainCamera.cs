using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    CommonChip core;
    void Awake()
    {
        core = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<CommonChip>();
        core.gameObject.layer = 6;
        core.gameObject.AddComponent<Rigidbody>();
        core.name = UIStrings.Core;
        //print($"Core: {core}");
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
