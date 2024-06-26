using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;

public class BaseScrollMenu : BasePanel, InputReceiver
{
    //public const int NUMDISPLAYITEMS = 7;
    //protected string[] myLabels = new string[] { };

    protected BaseItemScroll itemScroll;
    protected BaseScrollbar scrollbar;
    protected BaseButton[] btns;
    protected BaseInput input;

    BaseImage backgroundImage;

    public override void Setup()
    {
        base.Setup();
        this.vProp = new VirtualProp(PropType.Panel, 1f,
            new VirtualProp(PropType.Image, 1f,
                new VirtualProp(PropType.Panel, 0.8f, right,
                    new VirtualProp(PropType.Panel, 0.9f, typeof(BaseItemScroll),
                        new VirtualProp(PropType.Button, 1f)
                    ),
                    new VirtualProp(PropType.Scrollbar, -1f, right, right)
                ),
                new VirtualProp(PropType.Image, -1f, right,
                    new VirtualProp(PropType.Input, 0.60f),
                    new VirtualProp(PropType.Button, 0.2f),
                    new VirtualProp(PropType.Button, 0.2f)
                )
            )
        );
    }

    protected virtual void Start()
    {
        this.backgroundImage = this.GetComponentInChildren<BaseImage>();
        this.backgroundImage.image.color = new Color(0.9f, 0.9f, 0.9f);

        // TODO:
        // load all models from folder models and display them*
        // on click 'ok button': load the corresponding file
        // on click cancel: hide this menu
        foreach(TMP_Text t in this.GetComponentsInChildren<TMP_Text>())
        {
            t.fontSize = UIUtils.MediumFontSize;
        }
        this.itemScroll = this.GetComponentInChildren<BaseItemScroll>();
        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        // example of how to setup the scroller
        //this.itemScroll.SetupItemList(x => UnityEngine.Debug.Log(x), VariablePanel.NUMDISPLAYITEMS, this.myLabels);

        //this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        //this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        //this.itemScroll.Scroll(0f);
        //this.scrollbar.scrollbar.value = 0f;

        BaseButton[] allbtns = this.GetComponentsInChildren<BaseButton>();

        this.btns = allbtns[(^2)..(^0)];

        this.btns[0].text.text = "OK";
        this.btns[0].btn.onClick.RemoveAllListeners();

        this.btns[1].text.text = "CANCEL";
        this.btns[1].btn.onClick.RemoveAllListeners();

        this.input = this.GetComponentInChildren<BaseInput>();
        this.input.placeholder.SetText("Enter model name");

        this.gameObject.SetActive(false);
    }

    protected virtual void DeactivatePanel()
    {
        this.gameObject.SetActive(false);
        UIManager.instance.TurnMeOff(this);
    }

    public void ActivatePanel(string inputFieldText="")
    {
        this.input.input.SetTextWithoutNotify(inputFieldText);
        UIManager.instance.SwitchToMe(this);
        this.gameObject.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(this.input.input.gameObject);
        this.input.input.ActivateInputField();
        this.input.input.Select();
    }

    public virtual void OnEnable()
    {
        //this.itemScroll.virtualContainer.UpdateLabels(this.myLabels);
        //UIManager.instance.SwitchToMe(this);
    }

    void InputReceiver.OnStartReceiving()
    {
        this.SpecificOnStartReceiving();
    }
    protected virtual void SpecificOnStartReceiving() { }

    void InputReceiver.OnStopReceiving() { }
    bool InputReceiver.IsActive() => this.gameObject.activeSelf;

    void InputReceiver.HandleInputs()
    {
        if (!this.gameObject.activeSelf)
        {
            Debug.LogWarning($"Handling inputs on inactive objects.. SHOULD HANDLE IF SEEN MORE THAN A FEW TIMES");
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            this.DeactivatePanel();
        }

        if(this.btns is null || this.scrollbar is null)
        {
            PRINT.IPrint($"waiting for loading: scrollbar: {this.scrollbar == null}, btns: {this.btns == null}");
            return;
        }

        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            this.btns[0].btn.onClick.Invoke();
        }
        
        float scrollData = -Input.GetAxis("Mouse ScrollWheel");

        this.scrollbar.scrollbar.value = Mathf.Clamp01(this.scrollbar.scrollbar.value + 0.1f * scrollData);
    }

}

