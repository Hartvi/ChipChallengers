using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BaseImage : TopProp
{
    public Image image;
    protected override void Setup()
    {
        this.image = GetComponent<Image>();
    }

}
