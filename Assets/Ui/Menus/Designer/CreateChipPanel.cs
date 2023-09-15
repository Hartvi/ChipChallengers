using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class CreateChipPanel : BasePanel
{
    BaseButton[] bbtns;
    EditorMenu editorMenu;

    protected override void Setup()
    {
        base.Setup();
        this.vProp = 
            new VirtualProp(PropType.Panel, 1f,
                new VirtualProp(PropType.Panel, 0.5f,
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f),
                    new VirtualProp(PropType.Button, 1 / 8f)
                )
            );
    }

    void Start()
    {
        this.editorMenu = this.GetComponentInParent<EditorMenu>();
        this.bbtns = this.GetComponentsInChildren<BaseButton>();

        if(this.bbtns.Length != VChip.chipNames.Length)
        {
            throw new ArgumentException($"Buttons != chipNames: {bbtns.Length} != {VChip.chipNames}.");
        }

        for(int i = 0; i < this.bbtns.Length; ++i)
        {
            this.bbtns[i].text.SetText(VChip.chipNames[i]);
            // add virtual chip to selected chip from selected direction
            // TODO
            //bbtns[i].btn.onClick.AddListener();
        }
        this.gameObject.SetActive(false);
    }

    public void Display(Vector2 clickPosition, LocalDirection dir, VChip selectedVChip)
    {
        for(int i = 0; i < bbtns.Length; ++i)
        {
            int _i = i;
            bbtns[i].btn.onClick.RemoveAllListeners();
            bbtns[i].btn.onClick.AddListener(() => BtnCallback(bbtns[_i].text.text, dir, selectedVChip));
        }
        this.gameObject.SetActive(true);
        this.gameObject.RT().position = clickPosition - this.gameObject.RT().sizeDelta*0.5f;
    }

    void BtnCallback(string chipName, LocalDirection localDirection, VChip parent)
    {
        VChip newChip = new VChip(chipName, localDirection, parent);
        VModel vm = CommonChip.ClientCore.VirtualModel;
        vm.chips = vm.chips.Concat(new VChip[] { newChip }).ToArray();

        CommonChip.ClientCore.TriggerSpawn(vm, true);

        this.editorMenu.SelectVChip(newChip.rChip.equivalentVirtualChip.id);
        this.gameObject.SetActive(false);
    }
}
