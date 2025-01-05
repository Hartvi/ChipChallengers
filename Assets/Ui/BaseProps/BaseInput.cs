using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseInput : TopProp
{
    public TMP_InputField input;
    public TMP_Text placeholder;
    public Image image;
    public override void Setup()
    {
        this.input = this.GetComponent<TMP_InputField>();
        this.image = this.GetComponent<Image>();
        this.placeholder = this.GetComponentInChildren<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        //image.sprite.texture.SetPixelData()
        //input.targetGraphic.
    }
}
