using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CowlAspect : BaseAspect
{
    Transform parentTf;
    Transform inverseTf;
    void Start()
    {
        this.gameObject.layer = 7;
        GameObject.Destroy(this.gameObject.GetComponent<Joint>());
        GameObject.Destroy(this.gameObject.GetComponent<Rigidbody>());
        GeometricChip cc = this.myChip;
        while (cc.equivalentVirtualChip.ChipType == VChip.cowlStr)
        {
            cc = cc.parentChip;
        }
        parentTf = cc.transform;
        // TODO: create only one object per parent of all cowls
        inverseTf = new GameObject().transform;
        inverseTf.position = parentTf.position;
        inverseTf.rotation = parentTf.rotation;
        this.transform.SetParent(inverseTf);
    }


    public override void RuntimeFunction()
    {
        if (parentTf != null && inverseTf != null)
        {
            inverseTf.position = parentTf.position;
            inverseTf.rotation = parentTf.rotation;
        }
    }

    void OnDestroy()
    {
        if (this.inverseTf != null)
        {
            Destroy(this.inverseTf.gameObject);
        }
    }
}
