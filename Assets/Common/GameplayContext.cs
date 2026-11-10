using System;

// GameplayContext remembers regular sandbox or the selected scenario independently of runtime objects and menus.
public class GameplayContext
{
    public string ScenarioName { get; private set; }
    public bool IsScenario => this.ScenarioName != null;
    public int Revision { get; private set; }

    // SelectScenario remembers a scenario for each subsequent return from the editor to play mode.
    public void SelectScenario(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A scenario name is required.", nameof(name)); }
        this.ScenarioName = name;
        this.Revision++;
    }

    // SelectSandbox clears the selected scenario when the player explicitly chooses regular sandbox.
    public void SelectSandbox()
    {
        this.ScenarioName = null;
        this.Revision++;
    }

    // Retry restarts the selected scenario or resets the model in regular sandbox without changing modes.
    public void Retry(Action<string> restartScenario, Action resetSandbox)
    {
        if (this.IsScenario) { restartScenario(this.ScenarioName); }
        else { resetSandbox(); }
    }

    // MatchesScenario identifies queued loads that still belong to the current scenario selection.
    public bool MatchesScenario(string name, int revision) => this.IsScenario &&
        this.Revision == revision && string.Equals(this.ScenarioName, name, StringComparison.Ordinal);
}
