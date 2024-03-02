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
    float totalMenuHeight;
    public LocalDirection localDir;

    public override void Setup()
    {
        base.Setup();
        this.vProp =
            new VirtualProp(PropType.Panel, 1f,
                new VirtualProp(PropType.Panel, 0.5f,
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f),
                    new VirtualProp(PropType.Button, 1 / 9f)
                )
            );
    }

    void Start()
    {
        this.editorMenu = this.GetComponentInParent<EditorMenu>();
        this.bbtns = this.GetComponentsInChildren<BaseButton>();

        for (int i = 0; i < VChip.chipNames.Length; ++i)
        {
            this.bbtns[i].text.SetText(VChip.chipNames[i]);
        }

        BaseButton pb = this.bbtns[this.bbtns.Length - 1];
        pb.text.SetText(UIStrings.Paste);
        this.totalMenuHeight = pb.RT.sizeDelta.y * this.bbtns.Length;

        this.gameObject.SetActive(false);
    }

    public void Display(Vector2 clickPosition, LocalDirection dir, VChip selectedVChip)
    {
        // TODO: finish this: right now it just hides the other options, but still enables ctrl+v copy-paste
        // GOAL: enable ONLY cowls to be children of cowls
        if (selectedVChip.ChipType == VChip.cowlStr)
        {
            for (int i = 0; i < VChip.chipNames.Length - 1; ++i)
            {
                this.bbtns[i].gameObject.SetActive(false);
            }
            BaseButton bb = this.bbtns[this.bbtns.Length - 1];
            bb.gameObject.SetActive(false);
        }
        else
        {
            for (int i = 0; i < VChip.chipNames.Length; ++i)
            {
                this.bbtns[i].btn.gameObject.SetActive(true);
            }
        }
        // set direction so pasting in EditorMenu can work from the ctrl+v shortcut
        this.localDir = dir;

        for (int i = 0; i < VChip.chipNames.Length; ++i)
        {
            int _i = i;
            this.bbtns[i].btn.onClick.RemoveAllListeners();
            this.bbtns[i].btn.onClick.AddListener(() => BtnCallback(this.bbtns[_i].text.text, dir, selectedVChip));
        }

        BaseButton pb = this.bbtns[this.bbtns.Length - 1];
        pb.btn.onClick.RemoveAllListeners();
        pb.btn.onClick.AddListener(() => this.PasteCallback(selectedVChip, dir, false));
        this.gameObject.SetActive(true);

        // offset the panel so that it never goes out of screen
        Vector2 size = this.gameObject.RT().sizeDelta;
        Vector2 shiftVector = new Vector2(Mathf.Max(0f, size.x - clickPosition.x), Mathf.Max(0f, this.totalMenuHeight - clickPosition.y));
        this.gameObject.RT().position = clickPosition - size * 0.5f + shiftVector;
    }

    void BtnCallback(string chipName, LocalDirection localDirection, VChip parent)
    {
        VChip newChip = new VChip(chipName, localDirection, parent);
        CommonChip core = CommonChip.ClientCore;

        VModel vm = core.VirtualModel;
        vm.chips = vm.chips.Concat(new VChip[] { newChip }).ToArray();

        core.TriggerSpawn(vm, true);

        // TODO make this more secure:
        // HOW???
        this.editorMenu.selectedChip = this.editorMenu.highlighter.SelectVChip(newChip.rChip.equivalentVirtualChip.id);
        this.gameObject.SetActive(false);
    }

    public CommonChip PasteCallback(VChip selectedVChip, LocalDirection dir, bool mirror)
    {
        VChip pastedParentChip = Clipboard.AttachTo(selectedVChip, (int)dir, mirror);

        this.gameObject.SetActive(false);
        return this.editorMenu.highlighter.SelectVChip(selectedVChip.id);
    }
}
