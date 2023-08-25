using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelSwitcher : BasePanel
{
    BaseButton[] btns = new BaseButton[4];
    Type[] panels = new Type[] { typeof(ChipPanel), typeof(VariablePanel), typeof(ControlsPanel), typeof(ScriptPanel) };
    GameObject[] panelObjects;

    GameObjectSwitcher switcher;

    void Start()
    {
        this.btns = this.gameObject.GetComponentsInChildren<BaseButton>();

        this.panelObjects = new GameObject[panels.Length];
        this.switcher = new GameObjectSwitcher(panelObjects);

        for(int i = 0; i < this.btns.Length; ++i)
        {
            this.btns[i].text.SetText(UIStrings.EditorPanels[i]);
            this.btns[i].text.fontSize = UIUtils.SmallFontSize;

            this.panelObjects[i] = this.gameObject.GetComponentInParent<EditorMenu>().GetComponentInChildren(panels[i]).gameObject;
            
            // for some bloody reason `int` is a pointer by default
            int _i = i;
            this.btns[i].btn.onClick.AddListener(() => this.switcher.Switch(x => x == this.panelObjects[_i]));

        }
    }
    
}

