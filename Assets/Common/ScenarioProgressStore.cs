using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

// ScenarioProgressStore reads and writes scenario completion records in a versioned JSON file.
public class ScenarioProgressStore
{
    readonly string path;

    // ScenarioProgressStore uses the supplied path for scenario progress storage.
    public ScenarioProgressStore(string path)
    {
        this.path = path;
    }

    // Load loads existing progress or returns an empty save when no file exists.
    public ScenarioProgressSave Load()
    {
        if (!File.Exists(this.path)) { return new ScenarioProgressSave(); }
        var save = JsonConvert.DeserializeObject<ScenarioProgressSave>(File.ReadAllText(this.path));
        if (save == null || (save.saveVersion != 1 && save.saveVersion != 2) || save.scenarios == null ||
            save.scenarios.Any(x => x == null))
        {
            throw new InvalidDataException("Unsupported or invalid scenario progress file.");
        }
        return save;
    }

    // Record saves a valid finish once and updates its scenario completion and personal best.
    public bool Record(ScenarioMetadata metadata, ScenarioRunRecord run)
    {
        if (metadata == null || metadata.recordVersion < 1 || run == null ||
            string.IsNullOrWhiteSpace(run.scenarioHash) || string.IsNullOrWhiteSpace(run.scenarioFileName) ||
            string.IsNullOrWhiteSpace(run.runId) ||
            run.scenarioLogicVersion < 1 || (run.hasTime &&
            (double.IsNaN(run.timeSeconds) || double.IsInfinity(run.timeSeconds) || run.timeSeconds < 0)))
        {
            throw new ArgumentException("A valid scenario and completed run are required.");
        }
        if (metadata.rules == null || metadata.rules.physics == null || run.physics == null ||
            !run.physics.Matches(metadata.rules.physics) || run.rules == null ||
            !run.physics.Matches(run.rules.physics))
        {
            throw new ArgumentException("Recorded physics must match the scenario's required physics rules.");
        }
        var save = this.Load();
        var progress = save.scenarios.FirstOrDefault(x => x.scenarioHash == run.scenarioHash &&
            x.recordVersion == metadata.recordVersion && x.scenarioLogicVersion == run.scenarioLogicVersion &&
            x.lastRun != null && run.physics.Matches(x.lastRun.physics));
        if (progress == null)
        {
            progress = new ScenarioProgress {
                scenarioHash = run.scenarioHash, scenarioFileName = run.scenarioFileName,
                recordVersion = metadata.recordVersion,
                scenarioLogicVersion = run.scenarioLogicVersion
            };
            save.scenarios.Add(progress);
        }
        // Record ignores finish callbacks whose run ID matches the stored last or best run.
        if (progress.lastRun != null && progress.lastRun.runId == run.runId) { return false; }
        if (progress.bestRun != null && progress.bestRun.runId == run.runId) { return false; }
        progress.scenarioFileName = run.scenarioFileName;
        progress.completed = true;
        progress.completionCount++;
        progress.lastRun = run;
        if (run.hasTime && (progress.bestRun == null || !progress.bestRun.hasTime ||
            run.timeSeconds < progress.bestRun.timeSeconds))
        {
            progress.bestRun = run;
        }

        // Record writes a temporary file before replacing the previous save and retaining a backup.
        string directory = Path.GetDirectoryName(Path.GetFullPath(this.path));
        Directory.CreateDirectory(directory);
        // Record preserves the original ID-based save because historical Lua contents cannot be recovered from its runs.
        if (save.saveVersion == 1 && !File.Exists(this.path + ".v1.bak"))
            File.Copy(this.path, this.path + ".v1.bak");
        save.saveVersion = 2;
        string temporary = this.path + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(save, Formatting.Indented));
        if (File.Exists(this.path)) { File.Replace(temporary, this.path, this.path + ".bak"); }
        else { File.Move(temporary, this.path); }
        return true;
    }
}
