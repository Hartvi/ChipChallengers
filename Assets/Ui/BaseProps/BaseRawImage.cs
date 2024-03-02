using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseRawImage : TopProp
{
    public RawImage rawImage;
    public override void Setup()
    {
        rawImage = GetComponent<RawImage>();
    }

}
