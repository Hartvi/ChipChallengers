using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CowlAspect : BaseAspect
{
    void Start()
    {
        this.gameObject.layer = 7;
    }

}
