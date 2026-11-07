using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;
using Mirror;

public partial class GameScript
{
    public ScenarioMetadata Metadata { get; private set; }
    public string ScenarioHash { get; private set; }
    public string ScenarioFileName { get; private set; }
    public event Action<ScenarioRunRecord> ScenarioCompleted;
    ScenarioRunRecord activeAttemptRecord;
    // activeAttempt owns the physics-change subscription for the currently recorded run.
    ScenarioRunRecord activeAttempt {
        get { return this.activeAttemptRecord; }
        set {
            if (ReferenceEquals(this.activeAttemptRecord, value)) { return; }
            if (this.activeAttemptRecord != null) { PhysicsSettingsManager.Changing -= this.OnPhysicsSettingsChanging; }
            this.activeAttemptRecord = value;
            if (value != null) { PhysicsSettingsManager.Changing += this.OnPhysicsSettingsChanging; }
        }
    }
    CoreChip attemptCore;
    uint attemptResetCounter;
    double? finalRaceTime;
    string fallbackScenarioId;
    bool attemptCompleted;
    static GameScript physicsOwner;
    ScenarioPhysicsSettings previousPhysics;
    readonly Dictionary<Rigidbody, Vector2Int> previousBodySolvers = new Dictionary<Rigidbody, Vector2Int>();

    // RegisterScenarioCallbacks hashes the Lua bytes with empty map contents and registers the Lua recording functions.
    void RegisterScenarioCallbacks(string scenarioFileName, byte[] scenarioContents)
    {
        this.ScenarioHash = ScenarioContentHash.Compute(scenarioContents, Array.Empty<byte>());
        this.ScenarioFileName = Path.GetFileName(scenarioFileName);
        // fallbackScenarioId identifies files without explicit metadata by filename; Scenario({id=...}) supplies a permanent ID.
        this.fallbackScenarioId = string.IsNullOrWhiteSpace(scenarioFileName)
            ? "" : "legacy:" + Path.GetFileNameWithoutExtension(scenarioFileName);
        this.Metadata = new ScenarioMetadata { scenarioId = this.fallbackScenarioId };
        this.ApplyScenarioPhysics();
        ScenarioLuaCallbacks.Register(this.script, this.ConfigureScenario,
            this.BeginScenarioAttempt, this.CompleteAttempt, () => this.finalRaceTime, this.Race, this.RestartRace);
    }

    // ConfigureScenario applies validated scenario metadata before an attempt starts.
    void ConfigureScenario(Table metadata)
    {
        if (this.activeAttempt != null)
            throw new InvalidOperationException("Cannot change scenario metadata during an attempt.");
        this.Metadata = ScenarioLuaMetadata.Parse(metadata, this.fallbackScenarioId);
        this.ApplyScenarioPhysics();
    }

    // BeginScenarioAttempt validates the vehicle and snapshots its build and settings for a new attempt.
    public void BeginScenarioAttempt()
    {
        if (string.IsNullOrWhiteSpace(this.Metadata.scenarioId))
            throw new InvalidOperationException("Declare Scenario({id=...}) before starting an attempt.");
        if (NetworkClient.localPlayer == null)
            throw new InvalidOperationException("Player is not ready.");
        var core = CoreChip.ClientCoreChip;
        var model = core.VirtualModel;
        var counts = model.chips.GroupBy(chip => chip.ChipType)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ScenarioChipCount { chipType = group.Key, count = group.Count() }).ToList();
        this.CheckRules(counts);
        if (core.freeze || GameManager.RealTimeSettings.InMenu)
            throw new InvalidOperationException("Start the attempt in play mode.");
        this.ApplyScenarioPhysics();
        this.activeAttempt = new ScenarioRunRecord {
            scenarioHash = this.ScenarioHash,
            scenarioFileName = this.ScenarioFileName,
            runId = Guid.NewGuid().ToString("N"),
            gameVersion = Application.version,
            scenarioLogicVersion = ScenarioLuaMetadata.CurrentLogicVersion,
            completionType = this.Metadata.completionType,
            modelName = string.IsNullOrWhiteSpace(model.ModelName) ? GameManager.Instance.GetModel() : model.ModelName,
            modelSnapshot = model.ToLuaString(),
            chipCounts = counts,
            physics = PhysicsSettingsManager.Capture(),
            rules = new ScenarioRuleSnapshot {
                maxChips = this.Metadata.rules.maxChips,
                forbiddenChipTypes = new List<string>(this.Metadata.rules.forbiddenChipTypes),
                physics = this.Metadata.rules.physics.Copy()
            }
        };
        this.attemptCore = core;
        this.attemptResetCounter = core.srvResetCounter;
        this.finalRaceTime = null;
        this.attemptCompleted = false;
    }

    // CheckRules rejects vehicles exceeding the chip limit or containing forbidden chip types.
    void CheckRules(List<ScenarioChipCount> counts)
    {
        var rules = this.Metadata.rules;
        if (rules.maxChips >= 0 && counts.Sum(x => x.count) > rules.maxChips)
            throw new InvalidOperationException("Scenario allows at most " + rules.maxChips + " chips.");
        var forbidden = counts.Where(x => rules.forbiddenChipTypes.Any(type =>
            string.Equals(type, x.chipType, StringComparison.OrdinalIgnoreCase))).Select(x => x.chipType).ToArray();
        if (forbidden.Length > 0)
            throw new InvalidOperationException("Scenario forbids chip types: " + string.Join(", ", forbidden));
    }

    // AttemptIsValid checks live attempt state and optionally verifies the full model and body settings before saving.
    bool AttemptIsValid(bool verifySnapshot = false)
    {
        if (this.activeAttempt == null || NetworkClient.localPlayer == null || this.attemptCore == null)
            return false;
        var core = CoreChip.ClientCoreChip;
        var physics = this.activeAttempt.physics;
        bool stateIsValid = core == this.attemptCore && !core.freeze && !GameManager.RealTimeSettings.InMenu &&
            core.srvResetCounter == this.attemptResetCounter &&
            Time.fixedDeltaTime == physics.fixedDeltaTime &&
            Physics.defaultSolverIterations == physics.solverIterations &&
            Physics.defaultSolverVelocityIterations == physics.solverVelocityIterations;
        return stateIsValid && (!verifySnapshot ||
            (core.VirtualModel.ToLuaString() == this.activeAttempt.modelSnapshot &&
             this.previousBodySolvers.Keys.All(body => body == null ||
                 (body.solverIterations == physics.solverIterations && body.solverVelocityIterations == physics.solverVelocityIterations))));
    }

    // CompleteAttempt persists the completed attempt once and notifies listeners after saving succeeds.
    void CompleteAttempt(double? seconds)
    {
        // CompleteAttempt ignores repeated completion calls after the attempt has been saved.
        if (this.activeAttempt == null)
        {
            if (this.attemptCompleted) { return; }
            throw new InvalidOperationException("Call BeginScenarioAttempt before completing a scenario.");
        }
        if (!this.AttemptIsValid(verifySnapshot: true))
        {
            this.activeAttempt = null;
            throw new InvalidOperationException("Attempt invalidated by a reset, model/settings change or leaving play mode.");
        }
        if (seconds.HasValue && (double.IsNaN(seconds.Value) || double.IsInfinity(seconds.Value) || seconds.Value < 0))
            throw new ArgumentException("Finish time must be a finite, nonnegative number.");
        var run = this.activeAttempt;
        run.hasTime = seconds.HasValue;
        run.timeSeconds = seconds.GetValueOrDefault();
        run.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        run.finishedAtUtc = DateTimeOffset.UtcNow.ToString("O");
        var store = new ScenarioProgressStore(Path.Combine(Application.persistentDataPath, "scenario-progress.json"));
        store.Record(this.Metadata, run);
        this.activeAttempt = null;
        this.attemptCompleted = true;
        this.finalRaceTime = seconds;
        this.ScenarioCompleted?.Invoke(run);
    }

    // CheckAttemptValidity invalidates an active attempt when its vehicle, settings or play mode changes.
    void CheckAttemptValidity()
    {
        if (this.activeAttempt != null && !this.AttemptIsValid()) { this.InvalidateAttempt("model or play state changed"); }
    }

    // OnPhysicsSettingsChanging invalidates an active run before a global or per-body physics configuration changes.
    void OnPhysicsSettingsChanging(PhysicsSettingsChange change)
    {
        this.InvalidateAttempt("physics settings changed: " + change.Reason);
    }

    // InvalidateAttempt stops recording and race progression once and releases the attempt's physics subscription.
    void InvalidateAttempt(string reason)
    {
        if (this.activeAttempt == null) { return; }
        this.activeAttempt = null;
        this.isRace = false;
        this.raceProgress?.Fail();
        this.ReportScenarioError("Attempt invalidated (" + reason + "). " +
            (this.raceDefinition == null ? "Start a new attempt to retry." : "Press Ctrl+R to retry."));
    }

    // ReportScenarioError logs a scenario error and displays it briefly on screen.
    void ReportScenarioError(string message)
    {
        Debug.LogWarning(message);
        DisplaySingleton.Instance.DisplayText(text => {
            DisplaySingleton.ErrorMsgModification(text);
            text.SetText(message);
        }, 5f);
    }

    // ApplyScenarioPhysics preserves player settings once and applies the scenario rules to the active simulation.
    void ApplyScenarioPhysics()
    {
        if (physicsOwner != null && physicsOwner != this) { physicsOwner.RestorePhysics(); }
        if (this.previousPhysics == null)
        {
            this.previousPhysics = PhysicsSettingsManager.Capture();
            foreach (var body in FindObjectsOfType<Rigidbody>())
                this.previousBodySolvers[body] = new Vector2Int(body.solverIterations, body.solverVelocityIterations);
        }
        physicsOwner = this;
        var required = this.Metadata.rules.physics;
        PhysicsSettingsManager.Apply(required, "Scenario physics rules");
        foreach (var body in FindObjectsOfType<Rigidbody>()) { ApplyScenarioPhysicsToBody(body); }
    }

    // ApplyScenarioPhysicsToBody applies required solver settings to existing or newly rebuilt chip bodies.
    public static void ApplyScenarioPhysicsToBody(Rigidbody body)
    {
        if (physicsOwner == null || body == null) { return; }
        var owner = physicsOwner;
        if (!owner.previousBodySolvers.ContainsKey(body))
            owner.previousBodySolvers[body] = new Vector2Int(owner.previousPhysics.solverIterations, owner.previousPhysics.solverVelocityIterations);
        var required = owner.Metadata.rules.physics;
        PhysicsSettingsManager.ApplyBody(body, required.solverIterations, required.solverVelocityIterations, "Scenario body solvers");
    }

    // RestorePhysics restores player settings and the original solver settings of surviving bodies once.
    void RestorePhysics()
    {
        if (this.previousPhysics == null) { return; }
        if (physicsOwner == this)
        {
            physicsOwner = null;
            PhysicsSettingsManager.Apply(this.previousPhysics, "Restore player physics");
            foreach (var body in FindObjectsOfType<Rigidbody>())
            {
                var original = this.previousBodySolvers.TryGetValue(body, out var saved) ? saved :
                    new Vector2Int(this.previousPhysics.solverIterations, this.previousPhysics.solverVelocityIterations);
                PhysicsSettingsManager.ApplyBody(body, original.x, original.y, "Restore body solvers");
            }
        }
        this.previousPhysics = null;
        this.previousBodySolvers.Clear();
    }

    // OnDisable stops the scenario's attempt and gates and restores the player's physics settings.
    void OnDisable()
    {
        this.CancelRaceStart();
        this.activeAttempt = null;
        this.isRace = false;
        if (this.pendingRaceRestart != null) { StopCoroutine(this.pendingRaceRestart); }
        this.pendingRaceRestart = null;
        foreach (var sceneObject in this.SceneObjects.Values)
            if (sceneObject != null) { sceneObject.SetActive(false); }
        this.RestorePhysics();
    }

}
