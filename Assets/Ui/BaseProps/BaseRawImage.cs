using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseRawImage : TopProp
{
    public RawImage rawImage;
    protected override void Setup()
    {
        rawImage = GetComponent<RawImage>();
    }

}
