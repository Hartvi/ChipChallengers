using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class BaseButton : TopProp
{
    public Button btn;
    public TMP_Text text;
    public Image image;
    
    protected override void Setup()
    {
        this.btn = this.GetComponent<Button>();
        this.text = this.GetComponentInChildren<TMP_Text>();
        this.image = this.GetComponent<Image>();
        this.btn.onClick.AddListener(this.Execute);
    }
    
    protected virtual void Execute() { }

}
