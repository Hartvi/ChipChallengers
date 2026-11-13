using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared tutorial presentation, built through the same prefab/layout pipeline as the menus.
public class IntroductionOverlay : BasePanel
{
    VirtualProp modal, card, prompt, title, body, hint, primary, secondary, tertiary, skip;
    bool initialized;

    public override void Setup()
    {
        base.Setup();
        this.title = new VirtualProp(PropType.Text, 0.18f);
        this.body = new VirtualProp(PropType.Text, 0.43f);
        this.primary = new VirtualProp(PropType.Button, 0.1f);
        this.secondary = new VirtualProp(PropType.Button, 0.1f);
        this.tertiary = new VirtualProp(PropType.Button, 0.1f);
        this.card = new VirtualProp(PropType.Image, 0.7f, down,
            new VirtualProp(PropType.Panel, 0.03f),
            this.title, this.body, this.primary,
            new VirtualProp(PropType.Panel, 0.02f), this.secondary,
            new VirtualProp(PropType.Panel, 0.02f), this.tertiary);
        this.modal = new VirtualProp(PropType.Image, 1f, down,
            new VirtualProp(PropType.Panel, 0.12f),
            new VirtualProp(PropType.Panel, 0.76f, right,
                new VirtualProp(PropType.Panel, 0.15f), this.card));

        this.hint = new VirtualProp(PropType.Text, 0.73f);
        this.skip = new VirtualProp(PropType.Button, 0.27f);
        this.prompt = new VirtualProp(PropType.Image, 0.47f, down, this.hint, this.skip);
        this.vProp = new VirtualProp(PropType.Panel, 1f, zero,
            this.modal,
            new VirtualProp(PropType.Panel, 1f, down,
                new VirtualProp(PropType.Panel, 0.06f),
                new VirtualProp(PropType.Panel, 0.18f, right,
                    new VirtualProp(PropType.Panel, 0.02f), this.prompt)));
    }

    // Parent AddChildren constructs the hierarchy before Start or the first Show call.
    void Start() => this.Initialize();

    void Initialize()
    {
        if (this.initialized) { return; }
        this.initialized = true;
        var canvas = this.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        this.gameObject.AddComponent<GraphicRaycaster>();
        SetBackground(this.modal, new Color(0.02f, 0.04f, 0.07f, 0.8f), true);
        SetBackground(this.card, new Color(0.07f, 0.11f, 0.16f, 1f), true);
        SetBackground(this.prompt, new Color(0.04f, 0.08f, 0.13f, 0.94f), false);
        ConfigureText(this.title, UIUtils.LargeFontSize);
        ConfigureText(this.body, UIUtils.SmallFontSize);
        ConfigureText(this.hint, UIUtils.SmallFontSize);
        ((BaseText)this.hint.rProp).text.alignment = TextAlignmentOptions.MidlineLeft;
        foreach (var prop in new[] { this.primary, this.secondary, this.tertiary, this.skip })
        {
            var button = (BaseButton)prop.rProp;
            UIUtils.SetFontSize(button.text, UIUtils.SmallFontSize);
            button.text.enableAutoSizing = true;
            button.text.fontSizeMin = 12f;
            button.text.fontSizeMax = UIUtils.SmallFontSize;
        }
        this.modal.rProp.gameObject.SetActive(false);
        this.prompt.rProp.gameObject.SetActive(false);
    }

    public void ShowModal(string heading, string message, string primaryLabel, Action primaryAction,
        string secondaryLabel, Action secondaryAction, string tertiaryLabel = null, Action tertiaryAction = null)
    {
        this.Initialize();
        this.prompt.rProp.gameObject.SetActive(false);
        this.modal.rProp.gameObject.SetActive(true);
        ((BaseText)this.title.rProp).text.SetText(heading);
        ((BaseText)this.body.rProp).text.SetText(message);
        Bind(this.primary, primaryLabel, primaryAction);
        Bind(this.secondary, secondaryLabel, secondaryAction);
        Bind(this.tertiary, tertiaryLabel, tertiaryAction);
        if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
    }

    public void ShowPrompt(string message, Action onSkip)
    {
        this.Initialize();
        this.modal.rProp.gameObject.SetActive(false);
        this.prompt.rProp.gameObject.SetActive(true);
        ((BaseText)this.hint.rProp).text.SetText(message);
        Bind(this.skip, "Skip introduction", onSkip);
    }

    public void Hide()
    {
        this.Initialize();
        this.modal.rProp.gameObject.SetActive(false);
        this.prompt.rProp.gameObject.SetActive(false);
    }

    static void Bind(VirtualProp prop, string label, Action action)
    {
        var button = (BaseButton)prop.rProp;
        button.gameObject.SetActive(action != null);
        button.btn.onClick.RemoveAllListeners();
        if (action == null) { return; }
        button.text.SetText(label);
        button.btn.onClick.AddListener(() => {
            if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
            action();
        });
    }

    static void ConfigureText(VirtualProp prop, float size)
    {
        var text = ((BaseText)prop.rProp).text;
        UIUtils.SetFontSize(text, size);
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = size;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.margin = new Vector4(12f, 4f, 12f, 4f);
    }

    static void SetBackground(VirtualProp prop, Color color, bool blockPointer)
    {
        var image = ((BaseImage)prop.rProp).image;
        image.sprite = null;
        image.color = color;
        image.raycastTarget = blockPointer;
    }
}
