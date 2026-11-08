using System;

// WheelPhysics integrates wheel spin in seconds and radians independently of update frequency.
public static class WheelPhysics
{
    const float LinearResistancePerSecond = 0.35f;
    const float QuadraticResistancePerSecond = 1e-8f * 500f;

    // DiskInverseInertia calculates inverse spin inertia for a solid disk using its actual mass and radius.
    public static float DiskInverseInertia(float mass, float radius) => 2f / (mass * radius * radius);

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
