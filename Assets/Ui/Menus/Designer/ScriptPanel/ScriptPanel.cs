using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptPanel : BasePanel
{
    DragButton btn;
    BaseInput input;

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Button, 0.05f, typeof(DragButton)),
            new VirtualProp(PropType.Input, -1f)
        );
    }

    void Start()
    {
        this.input = this.GetComponentInChildren<BaseInput>();
        this.btn = this.GetComponentInChildren<DragButton>();

        this.btn.text.text = "<";
        this.input.placeholder.text = "Lua code...";

        this.btn.AddBtnHoldActions(new Action[] { this.DragBtn });
    }

    void DragBtn()
    {
        Vector2 mousePos = Input.mousePosition;
        Transform t = this.btn.transform;
        t.position = new Vector2(mousePos.x, t.position.y);

        // position offset between the two = half of input field + half of button
        float btnHalfWidth = t.gameObject.RT().sizeDelta.x*0.5f;
        
        // center of field without button is avg(screen width, mouse pos)

        this.input.transform.position = new Vector2((Screen.width + mousePos.x + btnHalfWidth) * 0.5f, this.input.transform.position.y);
        this.input.RT.sizeDelta = new Vector2(Screen.width - mousePos.x - btnHalfWidth, this.input.RT.sizeDelta.y);
         //+ mousePos.x*0.5f
    }

}
