using System;

// Preserve the original 50 Hz control tuning independently of rendering and physics frequencies.
public sealed class VehicleControlClock
{
    public const float TickSeconds = 1f / 50f;
    double accumulated;

    public void Reset() => this.accumulated = 0;

    public void Advance(double elapsed, Action<float> tick)
    {
        if (double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        this.accumulated += elapsed;
        const double period = 1.0 / 50;
        while (this.accumulated + 1e-9 >= period)
        {
            this.accumulated -= period;
            tick(TickSeconds);
        }
    }

    public static void Backstep(VVar[] variables)
    {
        foreach (var variable in variables)
        {
            if (!variable.hasChanged)
            {
                float difference = variable.defaultValue - variable.currentValue;
                variable.currentValue += Math.Sign(difference) * Math.Min(Math.Abs(difference), variable.backstep);
            }
            variable.hasChanged = false;
        }
    }
}
