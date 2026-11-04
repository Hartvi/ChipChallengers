using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class ScriptPanel : BaseSidePanel, InputReceiver
{
    DragButton btn;
    BaseInput input;
    bool initialized;
    Coroutine pendingRefresh;
    VModel boundModel;

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
        // Children have been built by now; appearance must not wait for networking.
        this.input = this.GetComponentInChildren<BaseInput>(true);
        this.btn = this.GetComponentInChildren<DragButton>(true);
        this.btn.text.SetText("<");
        this.btn.image.sprite = Resources.Load<Sprite>("UI/Art/tall_button");
        this.input.input.image.sprite = Resources.Load<Sprite>("UI/Art/white_square");
        this.input.placeholder.text = "Lua code...";
        this.input.placeholder.fontSize = UIUtils.SmallFontSize;
        this.input.input.onEndEdit.RemoveAllListeners();
        // Resolve the current core on edit; the player may have reconnected.
        this.input.input.onEndEdit.AddListener(this.SaveScript);
        this.input.input.textComponent.fontSize = UIUtils.SmallFontSize;
        this.input.input.lineType = TMPro.TMP_InputField.LineType.MultiLineNewline;
        this.btn.AddBtnHoldActions(new Action[] { this.DragBtn });
        this.initialized = true;
        this.pendingRefresh = StartCoroutine(this.RefreshWhenReady());
    }

    IEnumerator RefreshWhenReady()
    {
        // Always yield once so the coroutine handle can be assigned and child
        // Start methods can finish before binding.
        yield return null;
        while (NetworkClient.localPlayer == null ||
               CoreChip.ClientCoreChip.IsRebuilding ||
               CoreChip.ClientCoreChip.loadedModelString == null)
        {
            yield return null;
        }
        this.RefreshModel();
        this.pendingRefresh = null;
    }

    public void RefreshModel()
    {
        if (!this.initialized || NetworkClient.localPlayer == null) { return; }
        var core = CoreChip.ClientCoreChip;
        // An after-build callback also reaches here before loadedModelString is set.
        this.boundModel = core.VirtualModel;
        this.input.input.SetTextWithoutNotify(this.boundModel.script);
    }

    void SaveScript(string script)
    {
        if (NetworkClient.localPlayer == null) { return; }
        var core = CoreChip.ClientCoreChip;
        if (core.IsRebuilding) { return; }
        core.VirtualModel.script = script;
    }

    void DragBtn()
    {
        Vector2 mousePos = Input.mousePosition;
        Transform t = this.btn.transform;
        t.position = new Vector2(mousePos.x, t.position.y);

        // position offset between the two = half of input field + half of button
        float btnHalfWidth = t.gameObject.RT().sizeDelta.x * 0.5f;

        // center of field without button is avg(screen width, mouse pos)

        this.input.transform.position = new Vector2((Screen.width + mousePos.x + btnHalfWidth) * 0.5f, this.input.transform.position.y);
        this.input.RT.sizeDelta = new Vector2(Screen.width - mousePos.x - btnHalfWidth, this.input.RT.sizeDelta.y);
        //+ mousePos.x*0.5f
    }

    void OnEnable()
    {
        if (this.initialized && this.pendingRefresh == null)
        {
            this.pendingRefresh = StartCoroutine(this.RefreshWhenReady());
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (this.pendingRefresh != null) { StopCoroutine(this.pendingRefresh); }
        this.pendingRefresh = null;
        this.boundModel = null;
    }

    public bool IsSelected
    {
        get
        {
            if (this.input == null) { return false; }
            return this.input.input.isFocused;
        }
    }


    void Update()
    {
        if (this.initialized && NetworkClient.localPlayer != null)
        {
            var core = CoreChip.ClientCoreChip;
            if (!core.IsRebuilding && core.loadedModelString != null &&
                !ReferenceEquals(this.boundModel, core.VirtualModel))
            {
                this.RefreshModel();
            }
        }


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
