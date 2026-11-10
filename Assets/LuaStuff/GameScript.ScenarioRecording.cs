using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;
using Mirror;

public partial class GameScript
{
    public ChallengeMetadata Metadata { get; private set; }
    public string ChallengeHash { get; private set; }
    public string ChallengeFileName { get; private set; }
    public event Action<ChallengeRunRecord> ChallengeCompleted;
    ChallengeRunRecord activeAttemptRecord;
    // activeAttempt owns the physics-change subscription for the currently recorded run.
    ChallengeRunRecord activeAttempt {
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
    string fallbackChallengeId;
    bool attemptCompleted;
    static GameScript physicsOwner;
    ChallengePhysicsSettings previousPhysics;
    readonly Dictionary<Rigidbody, Vector2Int> previousBodySolvers = new Dictionary<Rigidbody, Vector2Int>();

    // RegisterChallengeCallbacks hashes the Lua bytes with empty map contents and registers the Lua recording functions.
    void RegisterChallengeCallbacks(string challengeFileName, byte[] challengeContents)
    {
        this.ChallengeHash = ChallengeContentHash.Compute(challengeContents, Array.Empty<byte>());
        this.ChallengeFileName = Path.GetFileName(challengeFileName);
        // fallbackChallengeId identifies files without explicit metadata by filename; Challenge({id=...}) supplies a permanent ID.
        this.fallbackChallengeId = string.IsNullOrWhiteSpace(challengeFileName)
            ? "" : "legacy:" + Path.GetFileNameWithoutExtension(challengeFileName);
        this.Metadata = new ChallengeMetadata { challengeId = this.fallbackChallengeId };
        this.ApplyChallengePhysics();
        ChallengeLuaCallbacks.Register(this.script, this.ConfigureChallenge,
            this.BeginChallengeAttempt, this.CompleteAttempt, () => this.finalRaceTime, this.Race, this.RestartRace);
    }

    // ConfigureChallenge applies validated challenge metadata before an attempt starts.
    void ConfigureChallenge(Table metadata)
    {
        if (this.activeAttempt != null)
            throw new InvalidOperationException("Cannot change challenge metadata during an attempt.");
        this.Metadata = ChallengeLuaMetadata.Parse(metadata, this.fallbackChallengeId);
        this.ApplyChallengePhysics();
    }

    // BeginChallengeAttempt validates the vehicle and snapshots its build and settings for a new attempt.
    public void BeginChallengeAttempt()
    {
        if (string.IsNullOrWhiteSpace(this.Metadata.challengeId))
            throw new InvalidOperationException("Declare Challenge({id=...}) before starting an attempt.");
        if (NetworkClient.localPlayer == null)
            throw new InvalidOperationException("Player is not ready.");
        var core = CoreChip.ClientCoreChip;
        var model = core.VirtualModel;
        var counts = model.chips.GroupBy(chip => chip.ChipType)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ChallengeChipCount { chipType = group.Key, count = group.Count() }).ToList();
        this.CheckRules(counts);
        if (core.freeze || GameManager.RealTimeSettings.InMenu)
            throw new InvalidOperationException("Start the attempt in play mode.");
        this.ApplyChallengePhysics();
        this.activeAttempt = new ChallengeRunRecord {
            challengeHash = this.ChallengeHash,
            challengeFileName = this.ChallengeFileName,
            runId = Guid.NewGuid().ToString("N"),
            gameVersion = Application.version,
            challengeLogicVersion = ChallengeLuaMetadata.CurrentLogicVersion,
            completionType = this.Metadata.completionType,
            modelName = string.IsNullOrWhiteSpace(model.ModelName) ? GameManager.Instance.GetModel() : model.ModelName,
            modelSnapshot = model.ToLuaString(),
            chipCounts = counts,
            physics = PhysicsSettingsManager.Capture(),
            rules = new ChallengeRuleSnapshot {
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
    void CheckRules(List<ChallengeChipCount> counts)
    {
        var rules = this.Metadata.rules;
        if (rules.maxChips >= 0 && counts.Sum(x => x.count) > rules.maxChips)
            throw new InvalidOperationException("Challenge allows at most " + rules.maxChips + " chips.");
        var forbidden = counts.Where(x => rules.forbiddenChipTypes.Any(type =>
            string.Equals(type, x.chipType, StringComparison.OrdinalIgnoreCase))).Select(x => x.chipType).ToArray();
        if (forbidden.Length > 0)
            throw new InvalidOperationException("Challenge forbids chip types: " + string.Join(", ", forbidden));
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
            throw new InvalidOperationException("Call BeginChallengeAttempt before completing a challenge.");
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
        var store = new ChallengeProgressStore(Path.Combine(Application.persistentDataPath, "challenge-progress.json"));
        store.Record(this.Metadata, run);
        this.activeAttempt = null;
        this.attemptCompleted = true;
        this.finalRaceTime = seconds;
        this.ChallengeCompleted?.Invoke(run);
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
        this.ReportChallengeError("Attempt invalidated (" + reason + "). " +
            (this.raceDefinition == null ? "Start a new attempt to retry." : "Press Ctrl+R to retry."));
    }

    // ReportChallengeError logs a challenge error and displays it briefly on screen.
    void ReportChallengeError(string message)
    {
        Debug.LogWarning(message);
        DisplaySingleton.Instance.DisplayText(text => {
            DisplaySingleton.ErrorMsgModification(text);
            text.SetText(message);
        }, 5f);
    }

    // ApplyChallengePhysics preserves player settings once and applies the challenge rules to the active simulation.
    void ApplyChallengePhysics()
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
        PhysicsSettingsManager.Apply(required, "Challenge physics rules");
        foreach (var body in FindObjectsOfType<Rigidbody>()) { ApplyChallengePhysicsToBody(body); }
    }

    // ApplyChallengePhysicsToBody applies required solver settings to existing or newly rebuilt chip bodies.
    public static void ApplyChallengePhysicsToBody(Rigidbody body)
    {
        if (physicsOwner == null || body == null) { return; }
        var owner = physicsOwner;
        if (!owner.previousBodySolvers.ContainsKey(body))
            owner.previousBodySolvers[body] = new Vector2Int(owner.previousPhysics.solverIterations, owner.previousPhysics.solverVelocityIterations);
        var required = owner.Metadata.rules.physics;
        PhysicsSettingsManager.ApplyBody(body, required.solverIterations, required.solverVelocityIterations, "Challenge body solvers");
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

    // OnDisable stops the challenge's attempt and gates and restores the player's physics settings.
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
