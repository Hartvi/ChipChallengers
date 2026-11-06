using System;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using Mirror;

public partial class GameScript
{
    ScenarioRaceDefinition raceDefinition;
    ScenarioRaceProgress raceProgress;
    readonly List<string> raceGates = new List<string>();
    Vector3 raceSpawnPosition;
    Quaternion raceSpawnRotation;
    ScenarioRacePoint previousRacePosition;
    double previousRaceSampleTime;
    int raceGeneration;
    UnityEngine.Coroutine pendingRaceRestart;

    // Race creates a race from named start/checkpoint/finish positions and shared gate appearance.
    public void Race(Table configuration) => this.Race(configuration, DynValue.Nil);

    // Race validates a course before replacing its gates and retains the original optional callback argument.
    void Race(Table configuration, DynValue legacyFinished)
    {
        var definition = ScenarioRaceDefinition.Parse(configuration, legacyFinished);
        this.ClearRace();
        this.raceDefinition = definition;
        this.raceSpawnPosition = this.AllObjects["player1"].transform.position;
        this.raceSpawnRotation = this.AllObjects["player1"].transform.rotation;
        foreach (var gate in definition.gates)
        {
            var table = new Table(this.script);
            table.Set("position", DynValue.NewTable(VectorTable(gate.position)));
            table.Set("size", DynValue.NewTable(VectorTable(new ScenarioRacePoint(gate.radius * 2, gate.radius * 2, gate.radius * 2))));
            var color = new Table(this.script);
            for (int i = 0; i < gate.color.Length; ++i) { color.Set(i + 1, DynValue.NewNumber(gate.color[i])); }
            table.Set("color", DynValue.NewTable(color));
            this.raceGates.Add(this.Spawn("gate", table));
        }
        this.PrepareRaceStart();
        this.script.Globals["RaceLoop"] = (Action)this.DisplayRaceTime;
    }

    // VectorTable converts a race position into the indexed vector format used by Spawn.
    Table VectorTable(ScenarioRacePoint point)
    {
        var table = new Table(this.script);
        table.Set(1, DynValue.NewNumber(point.x));
        table.Set(2, DynValue.NewNumber(point.y));
        table.Set(3, DynValue.NewNumber(point.z));
        return table;
    }

    // ClearRace stops pending retries and immediately disables old gates before their deferred destruction.
    void ClearRace()
    {
        this.raceGeneration++;
        if (this.pendingRaceRestart != null) { StopCoroutine(this.pendingRaceRestart); }
        this.pendingRaceRestart = null;
        this.isRace = false;
        this.activeAttempt = null;
        foreach (string name in this.raceGates)
        {
            if (this.SceneObjects.TryGetValue(name, out var gate))
            {
                gate.SetActive(false);
                Destroy(gate);
            }
            this.SceneObjects.Remove(name);
            this.AllObjects.Remove(name);
        }
        this.raceGates.Clear();
        this.raceDefinition = null;
        this.raceProgress = null;
    }

    // PrepareRaceStart resets timing and progress and exposes only the start gate for a new attempt.
    void PrepareRaceStart()
    {
        this.activeAttempt = null;
        this.attemptCompleted = false;
        this.finalRaceTime = null;
        this.times.Remove("race");
        this.raceProgress = new ScenarioRaceProgress(this.raceGates.Count);
        for (int i = 0; i < this.raceGates.Count; ++i)
        {
            if (i == 0) { this.Show(this.raceGates[i]); }
            else { this.Hide(this.raceGates[i]); }
        }
        this.previousRacePosition = Point(this.AllObjects["player1"].transform.position);
        this.previousRaceSampleTime = Time.fixedTimeAsDouble;
        this.isRace = true;
    }

    // RestartRace returns the vehicle to its original spawn and rearms the course after rebuilding finishes.
    public void RestartRace()
    {
        if (this.raceDefinition == null) { throw new InvalidOperationException("Call Race before RestartRace."); }
        if (NetworkClient.localPlayer == null) { throw new InvalidOperationException("Player is not ready."); }
        if (this.pendingRaceRestart != null) { StopCoroutine(this.pendingRaceRestart); }
        this.raceGeneration++;
        this.isRace = false;
        this.activeAttempt = null;
        this.finalRaceTime = null;
        this.attemptCompleted = false;
        var core = CoreChip.ClientCoreChip;
        this.AllObjects["player1"] = core.gameObject;
        this.ApplyScenarioPhysics();
        core.CmdResetCore();
        core.transform.SetPositionAndRotation(this.raceSpawnPosition, this.raceSpawnRotation);
        this.pendingRaceRestart = StartCoroutine(this.RearmRaceAfterReset(core));
    }

    // RearmRaceAfterReset waits for reset acknowledgement and model rebuilding before accepting gate crossings.
    IEnumerator RearmRaceAfterReset(CoreChip core)
    {
        yield return null;
        while (core != null && (core.IsRebuilding || core.srvResetCounter != core.cltResetCounter ||
               core.loadedModelString != core.modelString)) { yield return null; }
        this.pendingRaceRestart = null;
        if (core != null && this.raceDefinition != null) { this.PrepareRaceStart(); }
    }

    // TryRestartActiveRace routes the player's retry shortcut to the active scenario when it contains a race.
    public static bool TryRestartActiveRace()
    {
        if (physicsOwner == null || physicsOwner.raceDefinition == null) { return false; }
        physicsOwner.RestartRace();
        return true;
    }

    // FixedUpdate samples ordered gate crossings along vehicle motion instead of polling rendered-frame proximity.
    void FixedUpdate()
    {
        if (!this.isRace || this.raceDefinition == null || NetworkClient.localPlayer == null) { return; }
        var core = CoreChip.ClientCoreChip;
        var from = this.previousRacePosition;
        double fromTime = this.previousRaceSampleTime;
        var to = Point(core.transform.position);
        double toTime = Time.fixedTimeAsDouble;
        this.previousRacePosition = to;
        this.previousRaceSampleTime = toTime;
        if (core.IsRebuilding || core.srvResetCounter != core.cltResetCounter) { return; }
        if (this.activeAttempt != null && !this.AttemptIsValid()) { this.CheckAttemptValidity(); return; }
        int generation = this.raceGeneration;
        while (this.isRace && generation == this.raceGeneration && !this.raceProgress.Finished)
        {
            var gate = this.raceDefinition.gates[this.raceProgress.NextGate];
            double? fraction = ScenarioRaceProgress.CrossingFraction(from, to, gate.position, gate.radius);
            if (!fraction.HasValue) { break; }
            var crossing = new ScenarioRacePoint(
                (float)(from.x + (to.x - from.x) * fraction.Value),
                (float)(from.y + (to.y - from.y) * fraction.Value),
                (float)(from.z + (to.z - from.z) * fraction.Value));
            double crossingTime = fromTime + (toTime - fromTime) * fraction.Value;
            this.PassRaceGate(crossingTime);
            from = crossing;
            fromTime = crossingTime;
        }
    }

    // PassRaceGate records a valid crossing and invokes optional Lua callbacks after updating race state.
    void PassRaceGate(double crossingTime)
    {
        int index = this.raceProgress.NextGate;
        var definition = this.raceDefinition;
        int generation = this.raceGeneration;
        try
        {
            if (index == 0)
            {
                this.BeginScenarioAttempt();
                this.times["race"] = crossingTime;
            }
            double seconds = crossingTime - this.times["race"];
            if (index == this.raceGates.Count - 1) { this.CompleteAttempt(seconds); }
            if (generation != this.raceGeneration) { return; }
            this.raceProgress.Pass(index);
            this.Hide(this.raceGates[index]);
            if (this.raceProgress.Finished)
            {
                this.isRace = false;
                var style = new Table(this.script);
                style.Set("t", DynValue.NewNumber(3));
                this.Print($"FINISHED IN {seconds:F2} SECONDS - Ctrl+R to retry", style);
                if (!definition.onFinished.IsNil()) { this.script.Call(definition.onFinished, seconds); }
            }
            else
            {
                this.Show(this.raceGates[this.raceProgress.NextGate]);
                if (index == 0 && !definition.onStarted.IsNil()) { this.script.Call(definition.onStarted); }
                if (index > 0 && !definition.onCheckpoint.IsNil())
                    this.script.Call(definition.onCheckpoint, index, seconds);
            }
        }
        catch (Exception error)
        {
            if (generation == this.raceGeneration)
            {
                this.raceProgress.Fail();
                this.isRace = false;
            }
            this.ReportScenarioError(error.Message + " Press Ctrl+R to retry.");
        }
    }

    // DisplayRaceTime shows elapsed time and intermediate checkpoint progress for a running attempt.
    void DisplayRaceTime()
    {
        if (!this.times.ContainsKey("race")) { return; }
        var style = new Table(this.script);
        style.Set("x", DynValue.NewNumber(-0.7));
        style.Set("y", DynValue.NewNumber(0.7));
        style.Set("t", DynValue.NewNumber(0.1));
        int checkpoints = Math.Max(0, this.raceProgress.NextGate - 1);
        this.Print($"TIME: {this.GetTime("race"):F2} SECONDS - CHECKPOINTS: {checkpoints}/{this.raceGates.Count - 2}", style);
    }

    // Point converts a Unity position into the race geometry representation.
    static ScenarioRacePoint Point(Vector3 position) => new ScenarioRacePoint(position.x, position.y, position.z);
}
