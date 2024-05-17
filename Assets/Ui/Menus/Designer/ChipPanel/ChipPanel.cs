using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChipPanel : BasePanel, InputReceiver, IPointerEnterHandler, IPointerExitHandler
{
    int inputIndex = 0;
    TMP_InputField[] currentInputs;

    ItemBase NameItem;
    ItemBase ValueItem;
    ItemBase PopUpItem;

    BaseImage backgroundImage;
    bool insideChipPanel = false;
    int deselectTimer = -1;

    void Start()
    {
        this.backgroundImage = this.GetComponentInChildren<BaseImage>();

        var items = GetComponentsInChildren<ItemBase>();

        this.NameItem = items[0];
        this.ValueItem = items[1];
        ItemBase[] nameItems = this.NameItem.DisplayNItems<ItemBase>(10);
        ItemBase[] valueItems = this.ValueItem.DisplayNItems<ItemBase>(10);
        this.PopUpItem = items[2];
        ItemBase[] popUpItems = this.PopUpItem.DisplayNItems<ItemBase>(10);
        foreach (var v in popUpItems)
        {
            v.gameObject.SetActive(false);
        }

        // TEST
        //DisplayChipProperties(VChip.allPropertiesStr, VChip.allPropertiesDefaults);
        // TEST
        //DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);

        // Link highlighting in the editor in general to displaying in this menu
        EditorMenu em = this.GetComponentInParent<EditorMenu>();
        em.highlighter.SetHighlightCallbacks(new Action<VChip>[] { this.DisplayChip });

        // add default values so it renders fine at the start
        this.DisplayChip(CommonChip.ClientCore.equivalentVirtualChip);
    }

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Image, 1f,
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Panel, 0.5f, down,
                        new VirtualProp(PropType.Text, 1f / (float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                    ),
                    new VirtualProp(PropType.Panel, 0.5f, down,
                        new VirtualProp(PropType.Input, 1f / (float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                    ),
                    new VirtualProp(PropType.Panel, 0.25f, down,
                        new VirtualProp(PropType.Panel, 1f / (float)VChip.allPropertiesStr.Length, left,
                            new VirtualProp(PropType.Button, 1f, typeof(ItemBase))
                        )
                    )
                )
            )
        );
    }

    (TMP_Text[], TMP_InputField[]) DisplayChipProperties(string[] leftTexts, string[] rightTexts)
    {
        if (leftTexts.Length != rightTexts.Length)
        {
            throw new ArgumentException($"Left texts {leftTexts.Length} aren't the same length as right texts {rightTexts.Length}.");
        }

        TMP_InputField[] inputs = new TMP_InputField[rightTexts.Length];
        TMP_Text[] texts = new TMP_Text[rightTexts.Length];

        ItemBase[] nameItems = this.NameItem.DisplayNItems<ItemBase>(leftTexts.Length);
        ItemBase[] valueItems = this.ValueItem.DisplayNItems<ItemBase>(rightTexts.Length);

        for (int i = 0; i < leftTexts.Length; ++i)
        {
            var nameTxt = nameItems[i].GetComponent<TMP_Text>();

            nameTxt.SetText(leftTexts[i]);
            nameTxt.fontSize = UIUtils.SmallFontSize;

            nameTxt.horizontalAlignment = HorizontalAlignmentOptions.Center;
            nameTxt.verticalAlignment = VerticalAlignmentOptions.Middle;

            texts[i] = nameTxt;

            var valueTxt = valueItems[i].GetComponent<TMP_InputField>();
            valueTxt.SetTextWithoutNotify(rightTexts[i]);
            valueTxt.textComponent.fontSize = UIUtils.SmallFontSize;

            inputs[i] = valueTxt;
        }

        TopProp.StackFrom(this.NameItem.Siblings<ItemBase>(takeInactive: false));
        TopProp.StackFrom(this.ValueItem.Siblings<ItemBase>(takeInactive: false));

        return (texts, inputs);
    }

    public void DisplayChip(VChip vc)
    {
        string chipType = vc.ChipType;
        string[] newKeys = ArrayExtensions.AccessLikeDict(chipType, VChip.chipData.keys, VChip.chipData.values);
        string[] newValues = new string[newKeys.Length];

        for (int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if (val is not null)
            {
                newValues[i] = val.ToString();
            }
            else
            {
                newValues[i] = ArrayExtensions.AccessLikeDict(currentProperty, VChip.allPropertiesStr, VChip.allPropertiesDefaultsStrings);
            }
        }

        // This is so we can set it below using callbacks that index assuming the full length of properties
        vc.vals = newValues;
        vc.keys = newKeys;

        string[] displayKeys, displayValues;
        // if showing core, then hide its `type` field so as not to crash if it's changed
        bool isCore = (chipType == VChip.coreStr);
        int deltaLength = isCore ? -1 : 0;

        displayKeys = new string[newKeys.Length + deltaLength];
        displayValues = new string[newValues.Length + deltaLength];

        int k = 0;
        for (int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            if (currentProperty == VChip.typeStr && isCore) { continue; }

            displayKeys[k] = currentProperty;
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if (val is not null)
            {
                displayValues[k] = val.ToString();
            }
            else
            {
                displayValues[k] = ArrayExtensions.AccessLikeDict(currentProperty, VChip.allPropertiesStr, VChip.allPropertiesDefaultsStrings);
            }
            ++k;
        }

        (TMP_Text[] texts, TMP_InputField[] inputs) = this.DisplayChipProperties(displayKeys, displayValues);

        // for tabbing through the fields
        this.currentInputs = inputs;

        for (int i = 0; i < inputs.Length; ++i)
        {
            int _i = i;

            // TODO inputs[_i] was null????
            if (inputs[_i] is null)
            {
                Debug.LogWarning($"input {_i} IS NULL; text: {texts[_i]}");
                if (texts[_i] is not null)
                {
                    Debug.LogWarning($"text val: {texts[_i].text}");
                }
            }
            inputs[_i].onEndEdit.RemoveAllListeners();
            inputs[_i].onSelect.RemoveAllListeners();
            if (texts[_i].text == VChip.typeStr)
            {
                inputs[_i].onSelect.AddListener(
                    x =>
                    {
                        ItemBase[] popUpItems = this.PopUpItem.DisplayNItems<ItemBase>(VChip.chipNames.Length);
                        for (int k = 0; k < popUpItems.Length; ++k)
                        {
                            var btn = popUpItems[k].GetComponent<Button>();
                            btn.GetComponentInChildren<TMP_Text>().fontSize = UIUtils.SmallFontSize;
                            btn.onClick.RemoveAllListeners();
                            int _k = k;
                            btn.onClick.AddListener(
                                () =>
                                {
                                    inputs[_i].text = VChip.chipNames[_k];
                                    ItemBase[] popUpItems = this.PopUpItem.DisplayNItems<ItemBase>(VChip.chipNames.Length);
                                    for (int k = 0; k < popUpItems.Length; ++k)
                                    {
                                        popUpItems[k].gameObject.SetActive(false);
                                    }
                                    inputs[_i].onEndEdit.Invoke(inputs[_i].text);
                                }
                            );
                            var txt = btn.GetComponentInChildren<TMP_Text>();
                            txt.text = VChip.chipNames[k];
                        }
                        float xShift = inputs[_i].gameObject.RT().sizeDelta.x * 0.5f + this.PopUpItem.gameObject.RT().sizeDelta.x * 0.5f;
                        this.PopUpItem.transform.position = inputs[_i].transform.position - Vector3.right * xShift;
                        TopProp.StackFrom(this.PopUpItem.Siblings<ItemBase>(takeInactive: false));
                    }
                );
                inputs[_i].onDeselect.AddListener(
                    x =>
                    {
                        this.deselectTimer = 5;
                    }
                );
            }
            if (texts[_i].text == VChip.optionStr)
            {
                inputs[_i].onSelect.AddListener(
                    x =>
                    {
                        string[] optionNames = ArrayExtensions.AccessLikeDict(vc.ChipType, VChip.optionNames.keys, VChip.optionNames.values);
                        ItemBase[] popUpItems = this.PopUpItem.DisplayNItems<ItemBase>(optionNames.Length);
                        for (int k = 0; k < popUpItems.Length; ++k)
                        {
                            var btn = popUpItems[k].GetComponent<Button>();
                            btn.GetComponentInChildren<TMP_Text>().fontSize = UIUtils.SmallFontSize;
                            btn.onClick.RemoveAllListeners();
                            int _k = k;
                            btn.onClick.AddListener(
                                () =>
                                {
                                    inputs[_i].text = _k.ToString();
                                    ItemBase[] popUpItems = this.PopUpItem.DisplayNItems<ItemBase>(optionNames.Length);
                                    for (int k = 0; k < popUpItems.Length; ++k)
                                    {
                                        popUpItems[k].gameObject.SetActive(false);
                                    }
                                    inputs[_i].onEndEdit.Invoke(inputs[_i].text);
                                }
                            );
                            var txt = btn.GetComponentInChildren<TMP_Text>();
                            txt.text = optionNames[k];
                        }
                        float xShift = inputs[_i].gameObject.RT().sizeDelta.x * 0.5f + this.PopUpItem.gameObject.RT().sizeDelta.x * 0.5f;
                        this.PopUpItem.transform.position = inputs[_i].transform.position - Vector3.right * xShift;
                        TopProp.StackFrom(this.PopUpItem.Siblings<ItemBase>(takeInactive: false));
                    }
                );
                inputs[_i].onDeselect.AddListener(
                    x =>
                    {
                        this.deselectTimer = 5;
                    }
                );
            }
            inputs[_i].onEndEdit.AddListener(
                x =>
                {
                    string validityMsg = vc.CheckValidityOfPropertyForThisChip(texts[_i].text, x);
                    if (validityMsg is not null)
                    {
                        inputs[_i].SetTextWithoutNotify(vc.vals[_i]);
                        DisplaySingleton.Instance.DisplayText(ChipPanel.SetFormatMsg(validityMsg), 3f);
                    }
                    else
                    {
                        // TODO CALL BACK FOR CHIP CHANGED TO PROPAGATE TO MODEL CHANGED for all properties
                        //Debug.LogWarning($"TODO: make chip changed callback propagate to model changed callback for all properties.");
                        //print($"Setting value {x} for key {vc.keys[_i]} in chip {vc.ChipType}");
                        vc.vals[_i] = x;
                    }
                }
            );
        }
    }

    static Action<TMP_Text> SetFormatMsg(string msg)
    {
        Action<TMP_Text> action = x =>
        {
            x.SetText(msg);
            DisplaySingleton.ErrorMsgModification(x);
        };
        return action;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        this.insideChipPanel = false;
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        this.insideChipPanel = true;
    }

    void TabSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool shiftPressed = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);
            int shiftDirection = shiftPressed ? -1 : 1;

            this.inputIndex = this.inputIndex + shiftDirection;

            if (this.inputIndex < 0)
            {
                this.inputIndex = this.currentInputs.Length - 1;
            }
            else if (this.inputIndex >= this.currentInputs.Length)
            {
                this.inputIndex = 0;
            }

            this.currentInputs[this.inputIndex].Select();
        }
    }

    bool InputReceiver.IsActive()
    {
        return this.gameObject.activeSelf;
    }

    void Update()
    {
        if (this.deselectTimer > 0)
        {
            this.deselectTimer = this.deselectTimer - 1;
            if (this.deselectTimer == 0)
            {
                var ps = this.PopUpItem.Siblings<ItemBase>(takeInactive: false);
                foreach (var p in ps)
                {
                    p.gameObject.SetActive(false);
                }
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
        this.TabSwitch();
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (!this.insideChipPanel)
            {
                UIManager.instance.TurnMeOff(this);
            }
        }
    }

    void InputReceiver.OnStopReceiving()
    {
    }

    void InputReceiver.OnStartReceiving()
    {
    }
}

public class ModuloInt
{
    int i;
    int Max;

    public static ModuloInt operator ++(ModuloInt mi)
    {
        ++mi.i;

        if (mi.i >= mi.Max)
        {
            mi.i = 0;
        }
        return mi;
    }

    public static implicit operator int(ModuloInt d) => d.i;
    public static explicit operator ModuloInt(int d) => new ModuloInt(d);

    public ModuloInt(int d)
    {
        this.Max = d;
        this.i = d == 0 ? 0 : d % this.Max;
    }

    public ModuloInt(int d, int m)
    {
        this.i = m == 0 ? 0 : d % m;
        this.Max = m;
    }

    public override string ToString()
    {
        return this.i.ToString();
    }
}

