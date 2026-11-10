using System;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using Mirror;

public partial class GameScript
{
    ChallengeRaceDefinition raceDefinition;
    ChallengeRaceProgress raceProgress;
    readonly List<string> raceGates = new List<string>();
    Vector3 raceSpawnPosition;
    Quaternion raceSpawnRotation;
    int raceGeneration;
    UnityEngine.Coroutine pendingRaceRestart;
    UnityEngine.Coroutine pendingRaceStart;
    CoreChip heldRaceCore;

    // Race creates a race from named start/checkpoint/finish positions and shared gate appearance.
    public void Race(Table configuration) => this.Race(configuration, DynValue.Nil);

    // Race validates a course before replacing its gates and retains the original optional callback argument.
    void Race(Table configuration, DynValue legacyFinished)
    {
        var definition = ChallengeRaceDefinition.Parse(configuration, legacyFinished);
        this.ClearRace();
        this.raceDefinition = definition;
        foreach (var gate in definition.gates)
        {
            var table = new Table(this.script);
            table.Set("position", DynValue.NewTable(VectorTable(gate.position)));
            table.Set("size", DynValue.NewTable(VectorTable(new ChallengeRacePoint(gate.radius * 2, gate.radius * 2, gate.radius * 2))));
            var color = new Table(this.script);
            for (int i = 0; i < gate.color.Length; ++i) { color.Set(i + 1, DynValue.NewNumber(gate.color[i])); }
            table.Set("color", DynValue.NewTable(color));
            int index = this.raceGates.Count;
            string name = this.Spawn("gate", table);
            this.raceGates.Add(name);
            var gateObject = this.SceneObjects[name];
            var trigger = gateObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            gateObject.AddComponent<RaceGateTrigger>().Entered = other => this.EnterRaceGate(index, other);
            gateObject.SetActive(false);
        }
        this.pendingRaceRestart = StartCoroutine(this.RearmRaceAfterReset(CoreChip.ClientCoreChip, true));
        this.script.Globals["RaceLoop"] = (Action)this.DisplayRaceTime;
    }

    // VectorTable converts a race position into the indexed vector format used by Spawn.
    Table VectorTable(ChallengeRacePoint point)
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
        this.CancelRaceStart();
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
        this.raceProgress = new ChallengeRaceProgress(this.raceGates.Count);
        for (int i = 0; i < this.raceGates.Count; ++i)
        {
            if (i == 0) { this.Show(this.raceGates[i]); }
            else { this.Hide(this.raceGates[i]); }
        }
        this.isRace = true;
    }

    // RestartRace returns the vehicle to its original spawn and rearms the course after rebuilding finishes.
    public void RestartRace()
    {
        if (this.raceDefinition == null) { throw new InvalidOperationException("Call Race before RestartRace."); }
        this.CancelRaceStart();
        if (NetworkClient.localPlayer == null) { throw new InvalidOperationException("Player is not ready."); }
        if (this.pendingRaceRestart != null) { StopCoroutine(this.pendingRaceRestart); }
        this.raceGeneration++;
        this.isRace = false;
        this.activeAttempt = null;
        this.finalRaceTime = null;
        this.attemptCompleted = false;
        var core = CoreChip.ClientCoreChip;
        this.AllObjects["player1"] = core.gameObject;
        this.ApplyChallengePhysics();
        core.CmdResetPose(this.raceSpawnPosition, this.raceSpawnRotation);
        this.pendingRaceRestart = StartCoroutine(this.RearmRaceAfterReset(core));
    }

    // RearmRaceAfterReset waits for applied pose requests and rebuilding before capturing the initial spawn and enabling gates.
    IEnumerator RearmRaceAfterReset(CoreChip core, bool captureSpawn = false)
    {
        yield return null;
        while (core != null && (core.HasPendingPoseReset || core.IsRebuilding || core.srvResetCounter != core.cltResetCounter ||
               core.loadedModelString != core.modelString)) { yield return null; }
        this.pendingRaceRestart = null;
        if (core != null && this.raceDefinition != null)
        {
            if (captureSpawn)
            {
                this.raceSpawnPosition = core.rb.position;
                this.raceSpawnRotation = core.rb.rotation;
            }
            this.PrepareRaceStart();
        }
    }

    // EnterRaceGate accepts the next gate when Unity reports the local core entering its trigger.
    void EnterRaceGate(int index, Collider other)
    {
        if (!this.isActiveAndEnabled || !this.isRace || this.raceDefinition == null ||
            this.pendingRaceStart != null || NetworkClient.localPlayer == null ||
            this.raceProgress.Finished || index != this.raceProgress.NextGate) { return; }
        var core = CoreChip.ClientCoreChip;
        if (other.attachedRigidbody != core.GetComponent<Rigidbody>() ||
            core.HasPendingPoseReset || core.IsRebuilding || core.srvResetCounter != core.cltResetCounter) { return; }
        this.PassRaceGate(Time.timeAsDouble);
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
                this.pendingRaceStart = StartCoroutine(this.StartRaceCountdown(CoreChip.ClientCoreChip, generation));
                return;
            }
            double seconds = crossingTime - this.times["race"];
            if (index == this.raceGates.Count - 1) { this.CompleteAttempt(seconds); }
            if (generation != this.raceGeneration) { return; }
            this.raceProgress.Pass(index);
            this.Hide(this.raceGates[index]);
            if (this.raceProgress.Finished)
            {
                this.isRace = false;
                this.DisplayRaceText($"FINISHED IN {seconds:F2} SECONDS - Ctrl+R to retry", 3f, "race-result", false, 0.5f);
                if (!definition.onFinished.IsNil()) { this.script.Call(definition.onFinished, seconds); }
            }
            else
            {
                this.Show(this.raceGates[this.raceProgress.NextGate]);
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
            this.ReportChallengeError(error.Message + " Press Ctrl+R to retry.");
        }
    }

    // StartRaceCountdown stops the vehicle for one second and holds it through 3-2-1 before starting the timed attempt.
    IEnumerator StartRaceCountdown(CoreChip core, int generation)
    {
        this.heldRaceCore = core;
        core.CmdSetRaceStartHeld(true);
        double startedAt = Time.timeAsDouble;
        uint resetCounter = core.srvResetCounter;
        yield return null;
        while (!ChallengeRaceStartTiming.Ready(Time.timeAsDouble - startedAt))
        {
            if (!this.CanFinishRaceStart(core, generation, resetCounter))
            {
                this.pendingRaceStart = null;
                this.CancelRaceStart();
                this.isRace = false;
                this.raceProgress?.Fail();
                this.ReportChallengeError("Race start cancelled. Press Ctrl+R to retry.");
                yield break;
            }
            int number = ChallengeRaceStartTiming.CountdownAt(Time.timeAsDouble - startedAt);
            this.DisplayRaceStart(number == 0 ? "Get ready..." : number.ToString());
            yield return null;
        }
        this.pendingRaceStart = null;
        bool valid = this.CanFinishRaceStart(core, generation, resetCounter);
        this.CancelRaceStart();
        if (!valid) { this.isRace = false; yield break; }
        try
        {
            this.BeginChallengeAttempt();
            this.times["race"] = Time.timeAsDouble;
            this.raceProgress.Pass(0);
            this.Hide(this.raceGates[0]);
            this.Show(this.raceGates[1]);
            this.DisplayRaceStart("GO!");
            var callback = this.raceDefinition.onStarted;
            if (!callback.IsNil()) { this.script.Call(callback); }
        }
        catch (Exception error)
        {
            if (generation == this.raceGeneration)
            {
                this.raceProgress.Fail();
                this.isRace = false;
            }
            this.ReportChallengeError(error.Message + " Press Ctrl+R to retry.");
        }
    }

    // CanFinishRaceStart rejects a staged start after a reset, player replacement, menu transition or race replacement.
    bool CanFinishRaceStart(CoreChip core, int generation, uint resetCounter) => core != null &&
        NetworkClient.localPlayer != null && CoreChip.ClientCoreChip == core &&
        this.isActiveAndEnabled && this.isRace && generation == this.raceGeneration &&
        !core.freeze && !GameManager.RealTimeSettings.InMenu && core.srvResetCounter == resetCounter;

    // CancelRaceStart stops pending staging and releases its vehicle without leaving a cancelled challenge frozen.
    void CancelRaceStart()
    {
        if (this.pendingRaceStart != null) { StopCoroutine(this.pendingRaceStart); }
        this.pendingRaceStart = null;
        if (this.heldRaceCore != null && NetworkClient.active) { this.heldRaceCore.CmdSetRaceStartHeld(false); }
        this.heldRaceCore = null;
    }

    // DisplayRaceStart displays a short-lived staging or countdown message above the race timer.
    void DisplayRaceStart(string message)
    {
        this.DisplayRaceText(message, 0.25f, "race-start-countdown", true, 0.5f);
    }

    // DisplayRaceText gives challenge messages a centered box spanning 90 percent of the canvas width.
    void DisplayRaceText(string message, float duration, string id, bool large, float height)
    {
        DisplaySingleton.Instance.DisplayText(text => {
            if (large) { DisplaySingleton.BasicLargeModification(text); }
            else { DisplaySingleton.BasicMediumModification(text); text.color = Color.black; }
            var rect = text.rectTransform;
            var parent = (RectTransform)rect.parent;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(parent.rect.width * 0.9f, parent.rect.height * 0.2f);
            rect.anchoredPosition = new Vector2(0f, parent.rect.height * (height - 0.5f));
            text.enableWordWrapping = true;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.SetText(message);
        }, duration, id);
    }

    // DisplayRaceTime shows elapsed time and intermediate checkpoint progress for a running attempt.
    void DisplayRaceTime()
    {
        if (!this.times.ContainsKey("race")) { return; }
        int checkpoints = Math.Max(0, this.raceProgress.NextGate - 1);
        this.DisplayRaceText($"TIME: {this.GetTime("race"):F2} SECONDS - CHECKPOINTS: {checkpoints}/{this.raceGates.Count - 2}",
            0.1f, "race-timer", false, 0.85f);
    }
}
