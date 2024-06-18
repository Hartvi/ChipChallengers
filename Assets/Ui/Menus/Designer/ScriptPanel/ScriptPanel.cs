using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScriptPanel : BaseSidePanel, InputReceiver
{
    DragButton btn;
    BaseInput input;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Button, 0.05f, typeof(DragButton)),
            new VirtualProp(PropType.Input, -1f)
        );
    }

    void Start()
    {
        CommonChip core = CommonChip.ClientCore;
        this.input = this.GetComponentInChildren<BaseInput>();
        this.btn = this.GetComponentInChildren<DragButton>();

        this.btn.text.text = "<";
        this.btn.image.sprite = Resources.Load<Sprite>("UI/art/tall_button");

        this.input.input.image.sprite = Resources.Load<Sprite>("UI/Art/white_square");

        this.input.placeholder.text = "Lua code...";
        this.input.placeholder.fontSize = UIUtils.SmallFontSize;

        this.input.input.onEndEdit.RemoveAllListeners();
        this.input.input.onEndEdit.AddListener(x => core.VirtualModel.script = x);
        this.input.input.textComponent.fontSize = UIUtils.SmallFontSize;
        this.input.input.lineType = TMPro.TMP_InputField.LineType.MultiLineNewline;


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

    void OnEnable()
    {
        if (this.input is null) return;
        //if (this.input.input is null) return;
        //if (this.input.input is null) return;
        this.input.input.SetTextWithoutNotify(CommonChip.ClientCore.VirtualModel.script);
    }

    public bool IsSelected => this.input.input.isFocused;


    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            if (this.insideChipPanel)
            {
                UIManager.instance.SwitchToMe(this);
            }
        }
    }

    void InputReceiver.HandleInputs()
    {
        if (!this.insideChipPanel)
        {
            UIManager.instance.TurnMeOff(this);
        }
    }

    void InputReceiver.OnStopReceiving()
    {
    }

    void InputReceiver.OnStartReceiving()
    {
    }
    bool InputReceiver.IsActive()
    {
        return this.gameObject.activeSelf;
    }
}
