using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAspect : MonoBehaviour
{
    public CommonChip myChip;
    public Rigidbody rb => myChip.rb;
    public float value => myChip.value;
    public float brake => myChip.brake;


    protected virtual void Awake()
    {
        this.myChip = this.gameObject.GetComponent<CommonChip>();
        //if (SingleplayerMenu.Instance is null)
        //{
        //    throw new NullReferenceException("BaseAspect requires SingleplayerMenu to be initialized");
        //}
        SingleplayerMenu.RuntimeFunctions.Add(this);
    }

    public abstract void RuntimeFunction();

}

