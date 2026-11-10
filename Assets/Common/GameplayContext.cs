using System;

// GameplayContext remembers regular sandbox or the selected challenge independently of runtime objects and menus.
public class GameplayContext
{
    public string ChallengeName { get; private set; }
    public bool IsChallenge => this.ChallengeName != null;
    public int Revision { get; private set; }

    // SelectChallenge remembers a challenge for each subsequent return from the editor to play mode.
    public void SelectChallenge(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("A challenge name is required.", nameof(name)); }
        this.ChallengeName = name;
        this.Revision++;
    }

    // SelectSandbox clears the selected challenge when the player explicitly chooses regular sandbox.
    public void SelectSandbox()
    {
        this.ChallengeName = null;
        this.Revision++;
    }

    // Retry restarts the selected challenge or resets the model in regular sandbox without changing modes.
    public void Retry(Action<string> restartChallenge, Action resetSandbox)
    {
        if (this.IsChallenge) { restartChallenge(this.ChallengeName); }
        else { resetSandbox(); }
    }

    // MatchesChallenge identifies queued loads that still belong to the current challenge selection.
    public bool MatchesChallenge(string name, int revision) => this.IsChallenge &&
        this.Revision == revision && string.Equals(this.ChallengeName, name, StringComparison.Ordinal);
}
