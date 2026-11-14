using System;

public sealed class GunFireControl
{
    public const float MaximumPower = 100000f;
    public const int MaximumShotsPerSecond = 50;
    double charge;
    public float Charge => (float)this.charge;

    public static bool TryPower(float requested, out float power)
    {
        power = Math.Min(requested, MaximumPower);
        return !float.IsNaN(requested) && !float.IsInfinity(requested) && requested > 0;
    }

    public bool Step(float requested, bool trigger, float seconds, out float power)
    {
        if (!TryPower(requested, out power)) { this.charge = 0; return false; }
        this.charge = Math.Min(1, this.charge + Math.Min(MaximumShotsPerSecond, 1000.0 / power) * seconds);
        if (!trigger || this.charge < 1 - 1e-6) { return false; }
        this.charge = 0;
        return true;
    }
}
