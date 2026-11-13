using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VelocityHUD : BasePanel
{
    CommonChip _focus;
    CommonChip focus
    {
        set
        {
            if (value is not null)
            {
                this._focus = value;
            }
        }
        get
        {
            if(this._focus is null)
            {
                throw new NullReferenceException($"VelocityHUD: Focus is null. Cannot show velocity for null.");
            }
            return this._focus;
        }
    }

    float velocity => this._focus.rb.velocity.magnitude;
    Vector3 position => this._focus.transform.position;

    TMP_Text velocityText;
    [SerializeField] bool displayPosition = false;
    TMP_Text positionText;

    public override void Setup()
    {
        base.Setup();

        this.vProp = new VirtualProp(PropType.Panel, 1f, up,
            new VirtualProp(PropType.Panel, 0.1f, right,
                new VirtualProp(PropType.Text, 0.4f),
                new VirtualProp(PropType.Text, -1f)
            ),
            new VirtualProp(PropType.Panel, 0.1f, right,
                new VirtualProp(PropType.Text, 0.4f),
                new VirtualProp(PropType.Text, -1f)
            )
        );
    }

    void Start()
    {
        BaseText[] txts = this.GetComponentsInChildren<BaseText>();
        for(int i = 0; i < txts.Length; ++i)
        {
            UIUtils.SetFontSize(txts[i].text, UIUtils.SmallFontSize);
        }
        txts[0].text.SetText(UIStrings.Velocity);
        this.velocityText = txts[1].text;

        txts[2].text.SetText("");
        txts[3].text.SetText("");
        this.positionText = this.displayPosition ? txts[3].text : null;
        this.positionText?.SetText("");

        if(this.displayPosition) txts[2].text.SetText(UIStrings.Position);
    }

    void SetPosition()
    {
        this.positionText?.SetText(this.position.ToString().Trim('(', ')'));
    }

    void SetVelocity()
    {
        this.velocityText.SetText(UIUtils.DisplayFloat(this.velocity*3.6f) + " km/h");
    }

    public void SetFocus(CommonChip f)
    {
        this.focus = f;
    }

    void Update()
    {
        if(this._focus == null) { return; }
        SetVelocity();
        SetPosition();
    }
}
