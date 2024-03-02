using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlsPanel : BasePanel, InputReceiver
{
    BaseText Key1;
    BaseText[] Keys1;

    BaseText Action1;
    BaseText[] Actions1;

    BaseText Key2;
    BaseText[] Keys2;

    BaseText Action2;
    BaseText[] Actions2;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f, right,
            new VirtualProp(PropType.Image, 1f, down,
                new VirtualProp(PropType.Panel, 0.1f),
                new VirtualProp(PropType.Panel, -1f, right,
                    new VirtualProp(PropType.Panel, 0.5f, right,
                        new VirtualProp(PropType.Panel, 0.3f),
                        new VirtualProp(PropType.Panel, 0.4f, down,
                            new VirtualProp(PropType.Text, 0.1f)
                        ),
                        new VirtualProp(PropType.Panel, 0.3f, down,
                            new VirtualProp(PropType.Text, 0.1f)
                        )
                    )
                //new VirtualProp(PropType.Panel, -1f, down,
                //    new VirtualProp(PropType.Panel, 0.2f, right,
                //        new VirtualProp(PropType.Panel, 0.1f),
                //        new VirtualProp(PropType.Text, 0.4f),
                //        new VirtualProp(PropType.Text, 0.4f)
                //    )
                //)
                )
            )
        );
    }

    void Start()
    {
        BaseText[] txts = this.GetComponentsInChildren<BaseText>();
        this.Key1 = txts[0];
        this.Key1.text.fontSize = (UIUtils.MediumFontSize + UIUtils.SmallFontSize) / 2;
        this.Action1 = txts[1];
        this.Action1.text.fontSize = (UIUtils.MediumFontSize + UIUtils.SmallFontSize) / 2;
        //this.Key2 = txts[2];
        //this.Action2 = txts[3];

        int numTxts = UIStrings.ControlsKeys.Length;
        this.Keys1 = new BaseText[numTxts];
        this.Keys1[0] = this.Key1;

        this.Actions1 = new BaseText[numTxts];
        this.Actions1[0] = this.Action1;


        for (int i = 1; i < numTxts; ++i)
        {
            var k = GameObject.Instantiate<BaseText>(this.Key1);
            this.Keys1[i] = k;
            k.transform.SetParent(this.Key1.transform.parent);

            var a = GameObject.Instantiate<BaseText>(this.Action1);
            this.Actions1[i] = a;
            a.transform.SetParent(this.Action1.transform.parent);
        }

        for (int i = 0; i < numTxts; ++i)
        {
            var k = this.Keys1[i].text;
            k.SetText(UIStrings.ControlsKeys[i]);
            k.enableWordWrapping = false;

            var a = this.Actions1[i].text;
            a.SetText(UIStrings.ControlsActions[i]);
            a.enableWordWrapping = false;
        }

        TopProp.StackFrom<BaseText>(this.Keys1);
        TopProp.StackFrom<BaseText>(this.Actions1);

        this.gameObject.SetActive(false);
    }

    void InputReceiver.OnStartReceiving()
    {
        this.gameObject.SetActive(true);
    }

    void InputReceiver.OnStopReceiving()
    {
        this.gameObject.SetActive(false);
    }

    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        if (Input.anyKeyDown)
        {
            UIManager.instance.TurnMeOff(this);
        }
    }
}
