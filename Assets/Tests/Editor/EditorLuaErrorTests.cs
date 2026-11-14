using System.Collections;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class EditorLuaErrorTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject canvas, displayObject, panelObject, player;
    ScriptPanel panel;
    BaseInput input;
    DisplaySingleton display;
    CoreChip core;
    VModel model;
    object previousDisplay, previousPlayer;
    FieldInfo singleton = typeof(DisplaySingleton).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
    PropertyInfo localPlayer = typeof(NetworkClient).GetProperty("localPlayer");

    [SetUp]
    public void SetUp()
    {
        previousDisplay = singleton.GetValue(null); previousPlayer = localPlayer.GetValue(null);
        singleton.SetValue(null, null);
        canvas = new GameObject("Lua diagnostics canvas", typeof(Canvas));
        displayObject = new GameObject("Lua diagnostics display");
        display = displayObject.AddComponent<DisplaySingleton>();
        singleton.SetValue(null, display);
        typeof(DisplaySingleton).GetField("parentCanvas", Private).SetValue(display, canvas);
        panelObject = new GameObject("Lua diagnostics panel", typeof(RectTransform));
        panel = panelObject.AddComponent<ScriptPanel>();
        input = Object.Instantiate(UIUtils.Input, panelObject.transform).AddComponent<BaseInput>();
        input.Setup();
        typeof(ScriptPanel).GetField("input", Private).SetValue(panel, input);
        player = new GameObject("Lua diagnostics player"); player.SetActive(false);
        core = NetworkTestObjects.Add<CoreChip>(player);
        model = VModel.FromLuaModel("{chips={{id='a',Type='Core',orientation='0',parentId=''}},variables={},script='function Loop() end'}");
        typeof(CoreChip).GetField("_ClientVirtualModel", Private).SetValue(core, model);
        core.loadedModelString = model.ToLuaString();
        localPlayer.SetValue(null, player.GetComponent<NetworkIdentity>());
        model.Core.rChip = core;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(panelObject); Object.DestroyImmediate(player);
        Object.DestroyImmediate(displayObject); Object.DestroyImmediate(canvas);
        singleton.SetValue(null, previousDisplay); localPlayer.SetValue(null, previousPlayer);
    }

    void Invoke(string name, params object[] args) => typeof(ScriptPanel).GetMethod(name, Private).Invoke(panel, args);
    IDictionary Messages => (IDictionary)typeof(DisplaySingleton).GetField("displayedTexts", Private).GetValue(display);
    TMP_Text ErrorText => (TMP_Text)Messages["editor-lua-error"].GetType().GetField("TextComponent").GetValue(Messages["editor-lua-error"]);

    [Test]
    public void SyntaxErrorsStayVisiblePreserveDraftAndClearWhenCorrected()
    {
        input.input.SetTextWithoutNotify("function Loop(");
        Invoke("SaveScript", input.input.text);
        Assert.That(model.script, Is.EqualTo("function Loop("));
        Assert.That(input.input.text, Is.EqualTo(model.script));
        Assert.That(ErrorText.text, Is.Not.Empty);
        for (int i = 0; i < 3; ++i) Invoke("RefreshError");
        Assert.That(ErrorText.gameObject.activeSelf, Is.True);
        input.input.SetTextWithoutNotify("function Loop() end");
        Invoke("SaveScript", input.input.text);
        Invoke("RefreshError");
        Assert.That(Messages.Contains("editor-lua-error"), Is.False);
    }

    [Test]
    public void LoopErrorsAreShownAndOldErrorsDoNotFollowANewDraft()
    {
        model.script = "function Loop() error('editor runtime failure') end";
        input.input.SetTextWithoutNotify(model.script);
        core.scriptInstance = new ScriptInstance(core, model);
        core.scriptInstance.LinkSensors(model);
        core.scriptInstance.CallLoop();
        Invoke("CheckSyntax", model.script); Invoke("RefreshError");
        Assert.That(ErrorText.text, Does.Contain("editor runtime failure"));
        input.input.SetTextWithoutNotify("function Loop() end");
        Invoke("SaveScript", input.input.text);
        Invoke("RefreshError");
        Assert.That(Messages.Contains("editor-lua-error"), Is.False);
    }

    [Test]
    public void ServerDiagnosticCanBeDisplayedAndClosingPanelClearsIt()
    {
        input.input.SetTextWithoutNotify(model.script);
        typeof(CoreChip).GetField("serverLuaError", Private).SetValue(core, "server loop failed at line 4");
        Invoke("CheckSyntax", model.script); Invoke("RefreshError");
        Assert.That(ErrorText.text, Does.Contain("server loop failed"));
        Invoke("OnDisable");
        Assert.That(Messages.Contains("editor-lua-error"), Is.False);
    }
}
