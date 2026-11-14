using System;
using System.Collections;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;

// Validation-copy-only harness. Dormant unless explicitly selected on the command line.
public class ReviewSmokeProbe : MonoBehaviour
{
    static bool installed;
    string role, root;
    bool finished;
    float deadline;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--review-smoke");
        if (installed || index < 0 || index + 2 >= args.Length) return;
        installed = true;
        NetworkManager.singleton.headlessStartMode = HeadlessStartOptions.DoNothing;
        ((PortTransport)NetworkManager.singleton.transport).Port = 17991;
        var go = new GameObject("Review validation probe");
        DontDestroyOnLoad(go);
        var probe = go.AddComponent<ReviewSmokeProbe>();
        probe.role = args[index + 1];
        probe.root = args[index + 2];
        Directory.CreateDirectory(probe.root);
        probe.deadline = Time.realtimeSinceStartup + 150f;
        Application.logMessageReceived += probe.OnLog;
        probe.StartCoroutine(probe.Run());
    }

    void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            this.Finish(false, message + "\n" + stack);
    }
    void Update()
    {
        if (!this.finished && Time.realtimeSinceStartup > this.deadline) this.Finish(false, "Timed out: " + this.role);
    }
    void Mark(string name) => File.WriteAllText(Path.Combine(this.root, name), "ready");
    bool Marked(string name) => File.Exists(Path.Combine(this.root, name));
    bool Ready(CoreChip core) => core != null && !core.IsRebuilding && !core.HasPendingPoseReset &&
        core.loadedModelString != null && core.modelString == core.loadedModelString && core.srvResetCounter == core.cltResetCounter;
    IEnumerator Until(Func<bool> condition)
    {
        while (!this.finished && !condition()) yield return null;
        yield return null;
    }
    void Check(bool condition, string message) { if (!condition) this.Finish(false, message); }
    void Finish(bool pass, string message)
    {
        if (this.finished) return;
        this.finished = true;
        File.WriteAllText(Path.Combine(this.root, this.role + ".result.txt"), (pass ? "PASS: " : "FAIL: ") + message);
        Application.logMessageReceived -= this.OnLog;
        Application.Quit(pass ? 0 : 1);
    }

    IEnumerator Run()
    {
        yield return this.Until(() => SandboxMenu.myVMap.IsLoaded && UnityEngine.Object.FindObjectOfType<MainMenu>() != null);
        if (this.finished) yield break;
        var transport = NetworkManager.singleton.transport as PortTransport;
        this.Check(transport != null, "Scene lacks a configurable transport");
        if (this.finished) yield break;
        transport.Port = 17991;
        var manager = NetworkManager.singleton;
        manager.networkAddress = "127.0.0.1";
        GameManager.isHost = this.role == "host";
        GameManager.PlayContext.SelectSandbox();
        BaseMenu.SwitchToMenu(typeof(SandboxMenu));
        yield return this.Until(() => NetworkClient.localPlayer != null && this.Ready(CoreChip.ClientCoreChip));
        if (this.finished) yield break;
        CoreChip core = CoreChip.ClientCoreChip;
        yield return new WaitForSecondsRealtime(0.2f);
        if (this.role == "host")
        {
            this.Mark("host.ready");
            yield return this.Until(() => NetworkServer.connections.Count == 3);
            this.Check(SandboxMenu.myVMap.LoadNewMap("circuit1.obj"), "Host map change failed");
            yield return this.Until(() => this.Ready(core));
            this.Mark("host.map");
            yield return this.Until(() => this.Marked("client1.edits") && this.Marked("client2.edits"));
            SandboxMenu.PlayChallenge("01_industrial_test_track_race");
            this.Check(!GameManager.PlayContext.IsChallenge, "Multiplayer challenge bypassed solo restriction");
            this.Mark("disconnect.now");
            yield return this.Until(() => NetworkServer.connections.Count == 2);
            yield return new WaitForSecondsRealtime(0.5f);
            this.Check(core == CoreChip.ClientCoreChip && BaseMenu.allMenus.Any(menu => menu is SandboxMenu && menu.isSelected), "Remote disconnect removed host from sandbox");
            this.Mark("host.disconnect-checked");
            yield return this.Until(() => this.Marked("client2.disconnect-checked"));
            this.Finish(true, "Host joined two clients, synchronized map, preserved sandbox after remote disconnect, enforced solo challenges");
            yield break;
        }
        yield return this.Until(() => this.Marked("host.map") && SandboxMenu.myVMap.FileName == "circuit1" && this.Ready(core));
        string baseline = core.VirtualModel.ToLuaString();
        var edit = VModel.FromLuaModel(baseline);
        edit.script += "\n-- validation edit " + this.role;
        string edited = edit.ToLuaString();
        core.CmdLoadString(edited);
        yield return this.Until(() => this.Ready(core) && core.modelString == edited);
        core.CmdUndoHistory();
        yield return this.Until(() => this.Ready(core) && core.modelString == baseline);
        core.CmdRedoHistory();
        yield return this.Until(() => this.Ready(core) && core.modelString == edited);
        yield return new WaitForSecondsRealtime(0.2f);
        int audio = core.AllChips.Sum(chip => chip.GetComponents<AudioSource>().Length);
        int callbacks = core.RuntimeFunctions.Count;
        string before = string.Join(",", core.AllChips.Select(chip => chip.netId + ":" + chip.GetComponents<AudioSource>().Length));
        Debug.Log("REVIEW initial audio=" + audio + " callbacks=" + callbacks + " " + before);
        for (int i = 0; i < 3; ++i)
        {
            uint generation = core.srvResetCounter;
            core.CmdResetCore();
            yield return this.Until(() => core.srvResetCounter != generation && this.Ready(core));
            yield return new WaitForSecondsRealtime(0.1f);
            int after = core.AllChips.Sum(chip => chip.GetComponents<AudioSource>().Length);
            string details = string.Join(",", core.AllChips.Select(chip => chip.netId + ":" + chip.GetComponents<AudioSource>().Length));
            Debug.Log("REVIEW reset " + i + " audio=" + after + " callbacks=" + core.RuntimeFunctions.Count + " " + details);
            this.Check(after == audio, "Audio sources changed after reset: before=" + audio + " after=" + after + " initial=" + before + " current=" + details);
            this.Check(core.RuntimeFunctions.Count == callbacks, "Aspect registrations changed after reset: before=" + callbacks + " after=" + core.RuntimeFunctions.Count);
        }
        this.Mark(this.role + ".edits");
        yield return this.Until(() => this.Marked("disconnect.now"));
        if (this.role == "client1")
        {
            manager.StopClient();
            yield return new WaitForSecondsRealtime(0.5f);
            this.Finish(true, "Client map synchronization, remote undo/redo, three rebuilds without resource growth, clean disconnect");
        }
        else
        {
            yield return this.Until(() => this.Marked("host.disconnect-checked"));
            this.Check(NetworkClient.isConnected && CoreChip.ClientCoreChip == core && BaseMenu.allMenus.Any(menu => menu is SandboxMenu && menu.isSelected), "Other client disconnect removed remaining client from sandbox");
            this.Mark("client2.disconnect-checked");
            this.Finish(true, "Client map synchronization, remote undo/redo, three rebuilds without resource growth, remained in sandbox after other client left");
        }
    }
}
