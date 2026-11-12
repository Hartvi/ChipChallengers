using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A scene-local overlay: modal cards block pointer input; driving hints leave the view usable.
public class IntroductionOverlay : MonoBehaviour
{
    GameObject modal;
    GameObject prompt;
    TMP_Text title;
    TMP_Text body;
    TMP_Text hint;
    Button primary;
    Button secondary;
    Button tertiary;
    Button skip;

    public static IntroductionOverlay Create(Transform parent)
    {
        var root = Rect("Driving introduction", parent, Vector2.zero, Vector2.one);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var overlay = root.gameObject.AddComponent<IntroductionOverlay>();
        overlay.Build();
        return overlay;
    }

    void Build()
    {
        this.modal = Rect("Instruction overlay", this.transform, Vector2.zero, Vector2.one).gameObject;
        this.modal.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.8f);
        var card = Rect("Instruction card", this.modal.transform, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.88f));
        card.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.11f, 0.16f, 1f);
        this.title = Label("Title", card, new Vector2(0.06f, 0.81f), new Vector2(0.94f, 0.96f), 42f);
        this.body = Label("Instructions", card, new Vector2(0.07f, 0.38f), new Vector2(0.93f, 0.80f), 27f);
        this.primary = MakeButton("Continue", card, new Vector2(0.14f, 0.26f), new Vector2(0.86f, 0.36f));
        this.secondary = MakeButton("Skip", card, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.24f));
        this.tertiary = MakeButton("Back", card, new Vector2(0.14f, 0.02f), new Vector2(0.86f, 0.12f));

        this.prompt = Rect("Driving hint", this.transform, new Vector2(0.02f, 0.76f), new Vector2(0.49f, 0.94f)).gameObject;
        var background = this.prompt.AddComponent<Image>();
        background.color = new Color(0.04f, 0.08f, 0.13f, 0.94f);
        background.raycastTarget = false;
        this.hint = Label("Objective", this.prompt.transform, new Vector2(0.04f, 0.29f), new Vector2(0.96f, 0.95f), 25f);
        this.hint.alignment = TextAlignmentOptions.MidlineLeft;
        this.skip = MakeButton("Skip introduction", this.prompt.transform,
            new Vector2(0.04f, 0.04f), new Vector2(0.65f, 0.26f));
        this.Hide();
    }

    public void ShowModal(string heading, string message, string primaryLabel, Action primaryAction,
        string secondaryLabel, Action secondaryAction, string tertiaryLabel = null, Action tertiaryAction = null)
    {
        this.prompt.SetActive(false);
        this.modal.SetActive(true);
        this.title.SetText(heading);
        this.body.SetText(message);
        Bind(this.primary, primaryLabel, primaryAction);
        Bind(this.secondary, secondaryLabel, secondaryAction);
        Bind(this.tertiary, tertiaryLabel, tertiaryAction);
        if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
    }

    public void ShowPrompt(string message, Action onSkip)
    {
        this.modal.SetActive(false);
        this.prompt.SetActive(true);
        this.hint.SetText(message);
        Bind(this.skip, "Skip introduction", onSkip);
    }

    public void Hide()
    {
        this.modal.SetActive(false);
        this.prompt.SetActive(false);
    }

    static void Bind(Button button, string label, Action action)
    {
        button.gameObject.SetActive(action != null);
        button.onClick.RemoveAllListeners();
        if (action == null) { return; }
        button.GetComponentInChildren<TMP_Text>().SetText(label);
        button.onClick.AddListener(() => {
            if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
            action();
        });
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    static TMP_Text Label(string name, Transform parent, Vector2 min, Vector2 max, float maxSize)
    {
        var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = UIUtils.DefaultFont;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = maxSize;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    static Button MakeButton(string label, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = Rect(label, parent, min, max);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.31f, 0.43f, 1f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = Label("Label", rect, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), 27f);
        text.SetText(label);
        return button;
    }
}
