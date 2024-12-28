using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class BaseAspect : NetworkBehaviour
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
    }

    protected virtual void Start()
    {
        this.myChip.myCore.RuntimeFunctions.Add(this);
        //print($"Adding {this} to runtime functions");
    }

    //public override void OnStartClient()
    //{
    //    print($"STARTING ASPECT ON CLIENT");
    //    //    base.OnStartClient();
    //    //    this.myChip.myCore.RuntimeFunctions.Add(this);
    //    //    //SingleplayerMenu.RuntimeFunctions.Add(this);
    //}

    public abstract void RuntimeFunction();

    protected virtual void OnDestroy()
    {
    }

}

