using System;
using UnityEngine;

// PhysicsSettingsManager owns global timestep/default-solver writes and per-body solver writes and their callback lists.
public static class PhysicsSettingsManager
{
    static readonly PhysicsSettingsDispatcher dispatcher = new PhysicsSettingsDispatcher(Debug.LogException);
    public static event Action<PhysicsSettingsChange> Changing {
        add { dispatcher.Changing += value; }
        remove { dispatcher.Changing -= value; }
    }
    public static event Action<PhysicsSettingsChange> Changed {
        add { dispatcher.Changed += value; }
        remove { dispatcher.Changed -= value; }
    }

    // Capture reads the actual global timestep and default solver configuration into an independent snapshot.
    public static ChallengePhysicsSettings Capture() => new ChallengePhysicsSettings {
        fixedDeltaTime = Time.fixedDeltaTime,
        solverIterations = Physics.defaultSolverIterations,
        solverVelocityIterations = Physics.defaultSolverVelocityIterations
    };

    // Apply notifies subscribers before and after applying changed global physics settings.
    public static bool Apply(ChallengePhysicsSettings settings, string reason) => dispatcher.Apply(Capture, next => {
        Time.fixedDeltaTime = next.fixedDeltaTime;
        Physics.defaultSolverIterations = next.solverIterations;
        Physics.defaultSolverVelocityIterations = next.solverVelocityIterations;
    }, settings, reason);

    // SetFixedDeltaTime changes the simulation timestep while retaining the current default solver counts.
    public static bool SetFixedDeltaTime(float seconds, string reason)
    {
        var settings = Capture();
        settings.fixedDeltaTime = seconds;
        return Apply(settings, reason);
    }

    // SetSolverIterations updates default solver counts and existing bodies through physics-change notifications.
    public static void SetSolverIterations(int iterations, int velocityIterations, string reason)
    {
        var settings = Capture();
        settings.solverIterations = iterations;
        settings.solverVelocityIterations = velocityIterations;
        Apply(settings, reason);
        foreach (var body in UnityEngine.Object.FindObjectsOfType<Rigidbody>())
            ApplyBody(body, iterations, velocityIterations, reason);
    }

    // ApplyBody notifies subscribers before and after changing one rigidbody's solver configuration.
    public static bool ApplyBody(Rigidbody body, int iterations, int velocityIterations, string reason)
    {
        if (body == null) { return false; }
        var requested = Capture();
        requested.solverIterations = iterations;
        requested.solverVelocityIterations = velocityIterations;
        return dispatcher.Apply(() => new ChallengePhysicsSettings {
            fixedDeltaTime = Time.fixedDeltaTime, solverIterations = body.solverIterations,
            solverVelocityIterations = body.solverVelocityIterations
        }, next => {
            body.solverIterations = next.solverIterations;
            body.solverVelocityIterations = next.solverVelocityIterations;
        }, requested, reason, body);
    }
}
