using System;

public sealed class GunFireControl
{
    public const float MaximumPower = 100000f;
    public const int MaximumShotsPerSecond = 50;
    // Preserve the gun's tuned kick while impacts transfer the full projectile momentum.
    public const float RecoilScale = 0.01f;
    double charge;
    public float Charge => (float)this.charge;

    public static bool TryPower(float requested, out float power)
    {
        power = Math.Min(requested, MaximumPower);
        return !float.IsNaN(requested) && !float.IsInfinity(requested) && requested > 0;
    }

    public static float ProjectileMass(float requested)
    {
        if (!TryPower(requested, out float power)) { throw new ArgumentException("Projectile power must be finite and positive."); }
        // The standard 100-power shot historically used the prefab's one-kilogram bullet.
        return Math.Max(1e-5f, power * 0.01f);
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
