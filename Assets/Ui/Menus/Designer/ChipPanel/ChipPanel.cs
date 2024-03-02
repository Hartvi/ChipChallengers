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

    public override void Setup()
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

        for(int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if(val is not null)
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
        for(int i = 0; i < newKeys.Length; ++i)
        {
            string currentProperty = newKeys[i];
            if(currentProperty == VChip.typeStr && isCore) { continue; }

            displayKeys[k] = currentProperty;
            string val = ArrayExtensions.AccessLikeDict(currentProperty, vc.keys, vc.vals);

            if(val is not null)
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

        for(int i = 0; i < inputs.Length; ++i)
        {
            int _i = i;


            // IF OPTION OR CHIP TYPE
            //if (texts[_i].text == VChip.typeStr)
            //{
            //    TMP_Text t = texts[_i];
            //    var dropdowns = t.Siblings<DropdownItemScroll>(true);
            //    // if has dropdown, display it
            //    // position the drop down to be below/above the input field
            //    if(dropdowns.Length == 0)
            //    {
            //        //var d = Instantiate(Resources.Load<GameObject>("UI/Panel")).AddComponent<DropdownItemScroll>();
            //        //d.Setup();
            //        //d.AddChildren();
            //        //print($"dropdown: {d}");
            //        //d.transform.SetParent(t.transform.parent);

            //        ItemBase ib = inputs[_i].GetComponent<ItemBase>();
            //        VirtualProp ddvp = new VirtualProp(PropType.Panel, 1.0f, typeof(DropdownItemScroll));
            //        ib.vProp.Children.Add(ddvp);
            //        ddvp.Parent = ib.vProp;
            //        ib.AddChildren();

            //        dropdowns = t.Siblings<DropdownItemScroll>(true);
            //    }
            //    if (dropdowns.Length == 1)
            //    {
            //        DropdownItemScroll d = dropdowns[0];
            //        d.itemScroll.SetupItemList(x => { t.SetText(x); }, 3, VChip.chipNames);
            //        d.itemScroll.Scroll(0f);
            //        d.bScrollbar.scrollbar.value = 0f;
            //    } 
            //    //.transform.parent
            //}

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
                    string validityMsg = vc.CheckValidityOfPropertyForThisChip(texts[_i].text, x);
                    if(validityMsg is not null)
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

