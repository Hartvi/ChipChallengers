using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChipPanel : BasePanel
{
    int inputIndex = 0;
    TMP_InputField[] currentInputs;

    ItemBase NameItem;
    ItemBase ValueItem;

    BaseImage backgroundImage;

    void Start()
    {
        this.backgroundImage = this.GetComponentInChildren<BaseImage>();
        //this.backgroundImage.image.color = new Color(0.9f, 0.9f, 0.9f);

        var items = GetComponentsInChildren<ItemBase>();

        this.NameItem = items[0];
        this.ValueItem = items[1];
        ItemBase[] nameItems = this.NameItem.DisplayNItems<ItemBase>(10);
        ItemBase[] valueItems = this.ValueItem.DisplayNItems<ItemBase>(10);

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

    protected override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, 
                new VirtualProp(PropType.Image, 1f, 
                //new VirtualProp(PropType.Panel, 0.5f, down,
                //    new VirtualProp(PropType.Panel, 1/3f, right,
                //        new VirtualProp(PropType.Panel, 1/3f),
                //        new VirtualProp(PropType.Panel, 1/3f), 
                //        new VirtualProp(PropType.Panel, 1/3f)
                //    ),
                //    new VirtualProp(PropType.Panel, 1/3f, right,
                //        new VirtualProp(PropType.Panel, 1/3f),
                //        new VirtualProp(PropType.Panel, 1/3f), 
                //        new VirtualProp(PropType.Panel, 1/3f)
                //    ),
                //    new VirtualProp(PropType.Panel, 1/3f, right,
                //        new VirtualProp(PropType.Panel, 1/3f),
                //        new VirtualProp(PropType.Panel, 1/3f), 
                //        new VirtualProp(PropType.Panel, 1/3f)
                //    )
                //),
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Panel, 0.5f, down,
                        new VirtualProp(PropType.Text, 1f/(float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                    ),
                    new VirtualProp(PropType.Panel, 0.5f, down,
                        new VirtualProp(PropType.Input, 1f/(float)VChip.allPropertiesStr.Length, typeof(ItemBase))
                    )
                )
            )
        );
    }

    (TMP_Text[], TMP_InputField[]) DisplayChipProperties(string[] leftTexts, string[] rightTexts)
    {
        if(leftTexts.Length != rightTexts.Length)
        {
            throw new ArgumentException($"Left texts {leftTexts.Length} aren't the same length as right texts {rightTexts.Length}.");
        }

        TMP_InputField[] inputs = new TMP_InputField[rightTexts.Length];
        TMP_Text[] texts = new TMP_Text[rightTexts.Length];

        ItemBase[] nameItems = this.NameItem.DisplayNItems<ItemBase>(leftTexts.Length);
        ItemBase[] valueItems = this.ValueItem.DisplayNItems<ItemBase>(rightTexts.Length);

        for(int i = 0; i < leftTexts.Length; ++i)
        {
            var nameTxt = nameItems[i].GetComponent<TMP_Text>();

            nameTxt.SetText(leftTexts[i]);
            //nameTxt.fontSize = (UIUtils.MediumFontSize  + UIUtils.SmallFontSize)*0.5f;
            nameTxt.fontSize = UIUtils.SmallFontSize;

            nameTxt.horizontalAlignment = HorizontalAlignmentOptions.Center;
            nameTxt.verticalAlignment = VerticalAlignmentOptions.Middle;

            texts[i] = nameTxt;

            var valueTxt = valueItems[i].GetComponent<TMP_InputField>();
            valueTxt.SetTextWithoutNotify(rightTexts[i]);
            valueTxt.textComponent.fontSize = UIUtils.SmallFontSize;

            inputs[i] = valueTxt;

            //Debug.LogWarning($"Todo: add callback to update chip values.");
        }

        TopProp.StackFrom(this.NameItem.Siblings<ItemBase>(takeInactive: false));
        TopProp.StackFrom(this.ValueItem.Siblings<ItemBase>(takeInactive: false));

        return (texts, inputs);
    }

    public void DisplayChip(VChip vc)
    {
        //print($"DisplayChip: Chip type: {vc.ChipType}");
        string[] newKeys = ArrayExtensions.AccessLikeDict(vc.ChipType, VChip.chipData.keys, VChip.chipData.values);
        string[] newValues = new string[newKeys.Length];

        for(int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            //print($"current property: {currentProperty}");
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if(val is not null)
            {
                newValues[i] = val.ToString();
            }
            else
            {
                newValues[i] = ArrayExtensions.AccessLikeDict(currentProperty, VChip.allPropertiesStr, VChip.allPropertiesDefaultsStrings);
                //throw new ArgumentNullException($"Property {propertiesThisChipHas[i]} doesn't exist in chip {vc} of type {vc.ChipType}.");
            }
        }

        // This is so we can set it below using callbacks that index assuming the full length of properties
        vc.vals = newValues;
        vc.keys = newKeys;

        //PRINT.print(newKeys);
        //PRINT.print(newValues);

        (TMP_Text[] texts, TMP_InputField[] inputs) = this.DisplayChipProperties(newKeys, newValues);

        // for tabbing through the fields
        this.currentInputs = inputs;

        for(int i = 0; i < inputs.Length; ++i)
        {
            int _i = i;

            // TODO inputs[_i] was null????
            if (inputs[_i] is null)
            {
                Debug.LogWarning($"input {_i} IS NULL; text: {texts[_i]}");
                if(texts[_i] is not null)
                {
                    Debug.LogWarning($"text val: {texts[_i].text}");
                }
            }
            inputs[_i].onEndEdit.RemoveAllListeners();
            inputs[_i].onEndEdit.AddListener(
                x => {
                    //print($"chip type in callback: {vc.ChipType}, text: {texts[_i].text}, val: {x}");
                    string validityMsg = vc.CheckValidityOfPropertyForThisChip(texts[_i].text, x);
                    if(validityMsg is not null)
                    {
                        inputs[_i].SetTextWithoutNotify(vc.vals[_i]);
                        DisplaySingleton.Instance.DisplayText(ChipPanel.SetFormatMsg(validityMsg), 3f);
                    }
                    else
                    {
                        //print($"Changing val {vc.vals[_i]} to {inputs[_i].text}");
                        // TODO CALL BACK FOR CHIP CHANGED TO PROPAGATE TO MODEL CHANGED for all properties
                        Debug.LogWarning($"TODO: make chip changed callback propagate to model changed callback for all properties.");
                        //print($"{vc.ChipType}, {vc.id}, {vc.parentId}");
                        print($"Setting value {x} for key {vc.keys[_i]} in chip {vc.ChipType}");
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
    
    void Update()
    {
        TabSwitch();
    }

    void TabSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            bool shiftPressed = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift);
            int shiftDirection = shiftPressed ? -1 : 1;

            this.inputIndex = this.inputIndex + shiftDirection;

            if(this.inputIndex < 0)
            {
                this.inputIndex = this.currentInputs.Length - 1;
            }
            else if(this.inputIndex >= this.currentInputs.Length)
            {
                this.inputIndex = 0;
            }

            this.currentInputs[this.inputIndex].Select();
        }
    }
}

public class ModuloInt
{
    int i;
    int Max;

    public static ModuloInt operator ++(ModuloInt mi)
    {
        ++mi.i;

        if(mi.i >= mi.Max)
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

