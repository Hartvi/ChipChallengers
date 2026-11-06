using System;
using System.Collections.Generic;

// ScenarioProgressSave contains the save format version and progress records for all scenarios.
[Serializable]
public class ScenarioProgressSave
{
    public int saveVersion = 1;
    public List<ScenarioProgress> scenarios = new List<ScenarioProgress>();
}

// ScenarioProgress stores completion history and best/last results for one scenario and version combination.
[Serializable]
public class ScenarioProgress
{
    // scenarioId identifies the scenario independently of its display title or filename.
    public string scenarioId = "";
    public int recordVersion = 1;
    // scenarioLogicVersion separates personal bests produced by different processor versions.
    public int scenarioLogicVersion;
    public bool completed;
    public int completionCount;

    // bestRun stores the fastest valid timed finish and its player, vehicle and settings snapshots.
    public ScenarioRunRecord bestRun;
    // lastRun stores the most recent valid finish, including untimed completions.
    public ScenarioRunRecord lastRun;
}

// ScenarioRunRecord stores the result, player, vehicle and settings captured for one completed attempt.
[Serializable]
public class ScenarioRunRecord
{
    // runId identifies an attempt so repeated finish callbacks do not count it twice.
    public string runId = "";
    public string playerName = "";
    // finishedAtUtc stores the finish timestamp in ISO 8601 UTC format.
    public string finishedAtUtc = "";
    public string gameVersion = "";
    // scenarioLogicVersion records the processor/API version used for this attempt; zero means unknown.
    public int scenarioLogicVersion;
    public string completionType = "timeTrial";

    // hasTime distinguishes timed results from untimed completions.
    public bool hasTime;
    // timeSeconds stores unrounded elapsed seconds when hasTime is true.
    public double timeSeconds;

    // modelName identifies the vehicle captured at the start of the attempt.
    public string modelName = "";
    // modelSnapshot stores the starting build serialized by VModel.ToLuaString().
    public string modelSnapshot = "";
    public List<ScenarioChipCount> chipCounts = new List<ScenarioChipCount>();

    public ScenarioPhysicsSettings physics = new ScenarioPhysicsSettings();
    // rules preserves the restrictions applied to this run; the scenario definition controls validation.
    public ScenarioRuleSnapshot rules = new ScenarioRuleSnapshot();
}

// ScenarioChipCount stores the number of chips of one type in the starting vehicle.
[Serializable]
public class ScenarioChipCount
{
    // chipType stores the existing VChip type identifier, such as "Core", "Wheel" or "Jet".
    public string chipType = "";
    public int count;
}

// ScenarioPhysicsSettings stores the timestep and solver settings used during an attempt.
[Serializable]
public class ScenarioPhysicsSettings
{
    // fixedDeltaTime records the physics timestep used during the attempt.
    public float fixedDeltaTime;
    public int solverIterations;
    public int solverVelocityIterations;

    // CreateStandard supplies the shared 1000 Hz preset when a scenario omits physics rules.
    public static ScenarioPhysicsSettings CreateStandard() => new ScenarioPhysicsSettings {
        fixedDeltaTime = 0.001f, solverIterations = 1, solverVelocityIterations = 3
    };

    // Copy preserves physics values without sharing a mutable settings object.
    public ScenarioPhysicsSettings Copy() => new ScenarioPhysicsSettings {
        fixedDeltaTime = this.fixedDeltaTime, solverIterations = this.solverIterations,
        solverVelocityIterations = this.solverVelocityIterations
    };

    // Matches reports whether another settings snapshot describes the same simulation settings.
    public bool Matches(ScenarioPhysicsSettings other) => other != null &&
        this.fixedDeltaTime == other.fixedDeltaTime && this.solverIterations == other.solverIterations &&
        this.solverVelocityIterations == other.solverVelocityIterations;
}

// ScenarioRuleSnapshot stores the chip restrictions and required physics settings applied to an attempt.
[Serializable]
public class ScenarioRuleSnapshot
{
    public List<string> forbiddenChipTypes = new List<string>();
    // maxChips limits the total chip count; -1 means unlimited and zero allows no chips.
    public int maxChips = -1;
    // physics defines required simulation settings independently of player preferences.
    public ScenarioPhysicsSettings physics = ScenarioPhysicsSettings.CreateStandard();
}
