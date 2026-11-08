using System;

// WheelPhysics integrates wheel spin in seconds and radians independently of update frequency.
public static class WheelPhysics
{
    const float LinearResistancePerSecond = 1e-4f * 500f;
    const float QuadraticResistancePerSecond = 1e-8f * 500f;

    // ScaleInverseInertia preserves base-wheel tuning while inertia grows with mass and squared radius.
    public static float ScaleInverseInertia(float baseInverseInertia, float massScale, float radiusScale) =>
        baseInverseInertia / (massScale * radiusScale * radiusScale);

    // IntegrateSpin applies drive torque and elapsed-time resistance without braking past zero.
    public static float IntegrateSpin(float omega, float torque, float brake, float inverseInertia, float seconds)
    {
        float speed = Math.Abs(omega);
        float resistance = (brake * inverseInertia + LinearResistancePerSecond * speed +
            QuadraticResistancePerSecond * speed * speed) * seconds;
        return omega + torque * inverseInertia * seconds - Math.Sign(omega) * Math.Min(speed, resistance);
    }

    // ContactSpinDelta converts the tire's longitudinal force into its opposing wheel angular-velocity change.
    public static float ContactSpinDelta(float force, float radius, float inverseInertia, float seconds) =>
        -radius * force * inverseInertia * seconds;

    // SpinDegrees converts angular velocity in radians per second into an angle over the rendered interval.
    public static float SpinDegrees(float omega, float seconds) => omega * seconds * (180f / (float)Math.PI);
}
