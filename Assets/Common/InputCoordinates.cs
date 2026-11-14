using System;

public static class InputCoordinates
{
    public static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f :
        Math.Max(-1f, Math.Min(1f, value));

    public static float Normalize(float pixel, float viewportSize) => viewportSize > 0f ?
        Clamp(2f * pixel / viewportSize - 1f) : 0f;
}
