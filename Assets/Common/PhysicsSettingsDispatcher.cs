using System;

// PhysicsSettingsChange describes one configuration change and protects its before/after snapshots from listener edits.
public sealed class PhysicsSettingsChange
{
    readonly ChallengePhysicsSettings previous;
    readonly ChallengePhysicsSettings next;
    public ChallengePhysicsSettings Previous => this.previous.Copy();
    public ChallengePhysicsSettings Next => this.next.Copy();
    public string Reason { get; }
    public object Target { get; }

    // PhysicsSettingsChange captures the settings, change reason and optional rigidbody target for notifications.
    public PhysicsSettingsChange(ChallengePhysicsSettings previous, ChallengePhysicsSettings next, string reason, object target)
    {
        this.previous = previous.Copy();
        this.next = next.Copy();
        this.Reason = reason;
        this.Target = target;
    }
}

// PhysicsSettingsDispatcher notifies subscribers before and after validated physics configuration changes.
public sealed class PhysicsSettingsDispatcher
{
    public event Action<PhysicsSettingsChange> Changing;
    public event Action<PhysicsSettingsChange> Changed;
    readonly Action<Exception> reportError;
    bool applying;

    // PhysicsSettingsDispatcher accepts an error reporter so a failing listener cannot block other subscribers.
    public PhysicsSettingsDispatcher(Action<Exception> reportError = null)
    {
        this.reportError = reportError;
    }

    // Apply validates a requested configuration and notifies listeners around one actual settings write.
    public bool Apply(Func<ChallengePhysicsSettings> read, Action<ChallengePhysicsSettings> write,
        ChallengePhysicsSettings requested, string reason, object target = null)
    {
        if (this.applying) { throw new InvalidOperationException("Physics settings cannot be changed inside a physics settings callback."); }
        Validate(requested);
        var next = requested.Copy();
        var previous = read().Copy();
        if (previous.Matches(next)) { return false; }
        var change = new PhysicsSettingsChange(previous, next, reason, target);
        this.applying = true;
        try
        {
            this.Notify(this.Changing, change);
            write(next);
            this.Notify(this.Changed, change);
        }
        finally { this.applying = false; }
        return true;
    }

    // Validate rejects nonfinite timesteps and solver counts outside Unity's supported configuration range.
    static void Validate(ChallengePhysicsSettings settings)
    {
        if (settings == null || float.IsNaN(settings.fixedDeltaTime) || float.IsInfinity(settings.fixedDeltaTime) ||
            settings.fixedDeltaTime <= 0 || settings.solverIterations < 1 || settings.solverIterations > 255 ||
            settings.solverVelocityIterations < 1 || settings.solverVelocityIterations > 255)
        {
            throw new ArgumentException("Physics settings require a positive finite timestep and solver counts from 1 to 255.");
        }
    }

    // Notify invokes every registered listener and reports failures without cancelling the configuration change.
    void Notify(Action<PhysicsSettingsChange> listeners, PhysicsSettingsChange change)
    {
        if (listeners == null) { return; }
        foreach (Action<PhysicsSettingsChange> listener in listeners.GetInvocationList())
        {
            try { listener(change); }
            catch (Exception error) { this.reportError?.Invoke(error); }
        }
    }
}
