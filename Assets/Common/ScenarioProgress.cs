using System;
using System.Collections.Generic;

// ChallengeProgressSave contains the save format version and progress records for all challenges.
[Serializable]
public class ChallengeProgressSave
{
    public int saveVersion = 2;
    public List<ChallengeProgress> challenges = new List<ChallengeProgress>();
}

// ChallengeProgress stores completion history and best/last results for one challenge and version combination.
[Serializable]
public class ChallengeProgress
{
    // challengeHash identifies the Lua contents with empty map contents until map hashing is enabled.
    public string challengeHash = "";
    // challengeFileName records the filename, including its extension, of the most recent completed run.
    public string challengeFileName = "";
    public int recordVersion = 1;
    // challengeLogicVersion separates personal bests produced by different processor versions.
    public int challengeLogicVersion;
    public bool completed;
    public int completionCount;

    // bestRun stores the fastest valid timed finish and its player, vehicle and settings snapshots.
    public ChallengeRunRecord bestRun;
    // lastRun stores the most recent valid finish, including untimed completions.
    public ChallengeRunRecord lastRun;
}

// ChallengeRunRecord stores the result, player, vehicle and settings captured for one completed attempt.
[Serializable]
public class ChallengeRunRecord
{
    public string challengeHash = "";
    public string challengeFileName = "";
    // runId identifies an attempt so repeated finish callbacks do not count it twice.
    public string runId = "";
    public string playerName = "";
    // finishedAtUtc stores the finish timestamp in ISO 8601 UTC format.
    public string finishedAtUtc = "";
    public string gameVersion = "";
    // challengeLogicVersion records the processor/API version used for this attempt; zero means unknown.
    public int challengeLogicVersion;
    public string completionType = "timeTrial";

    // hasTime distinguishes timed results from untimed completions.
    public bool hasTime;
    // timeSeconds stores unrounded elapsed seconds when hasTime is true.
    public double timeSeconds;

    // modelName identifies the vehicle captured at the start of the attempt.
    public string modelName = "";
    // modelSnapshot stores the starting build serialized by VModel.ToLuaString().
    public string modelSnapshot = "";
    public List<ChallengeChipCount> chipCounts = new List<ChallengeChipCount>();

    public ChallengePhysicsSettings physics = new ChallengePhysicsSettings();
    // rules preserves the restrictions applied to this run; the challenge definition controls validation.
    public ChallengeRuleSnapshot rules = new ChallengeRuleSnapshot();
}

// ChallengeChipCount stores the number of chips of one type in the starting vehicle.
[Serializable]
public class ChallengeChipCount
{
    // chipType stores the existing VChip type identifier, such as "Core", "Wheel" or "Jet".
    public string chipType = "";
    public int count;
}

// ChallengePhysicsSettings stores the timestep and solver settings used during an attempt.
[Serializable]
public class ChallengePhysicsSettings
{
    // fixedDeltaTime records the physics timestep used during the attempt.
    public float fixedDeltaTime;
    public int solverIterations;
    public int solverVelocityIterations;

    // CreateStandard supplies the shared 1000 Hz preset when a challenge omits physics rules.
    public static ChallengePhysicsSettings CreateStandard() => new ChallengePhysicsSettings {
        fixedDeltaTime = 0.001f, solverIterations = 6, solverVelocityIterations = 1
    };

    // Copy preserves physics values without sharing a mutable settings object.
    public ChallengePhysicsSettings Copy() => new ChallengePhysicsSettings {
        fixedDeltaTime = this.fixedDeltaTime, solverIterations = this.solverIterations,
        solverVelocityIterations = this.solverVelocityIterations
    };

    // Matches reports whether another settings snapshot describes the same simulation settings.
    public bool Matches(ChallengePhysicsSettings other) => other != null &&
        this.fixedDeltaTime == other.fixedDeltaTime && this.solverIterations == other.solverIterations &&
        this.solverVelocityIterations == other.solverVelocityIterations;
}

// ChallengeRuleSnapshot stores the chip restrictions and required physics settings applied to an attempt.
[Serializable]
public class ChallengeRuleSnapshot
{
    public List<string> forbiddenChipTypes = new List<string>();
    // maxChips limits the total chip count; -1 means unlimited and zero allows no chips.
    public int maxChips = -1;
    // physics defines required simulation settings independently of player preferences.
    public ChallengePhysicsSettings physics = ChallengePhysicsSettings.CreateStandard();
}
