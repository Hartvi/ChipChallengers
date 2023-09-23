using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsData : MonoBehaviour
{
    public const float p0 = 101325f; // sea level pressure [Pa]
    public const float T0 = 288.15f; // sea level temperature [K]
    public const float g = 9.81f; // gravitational acceleration
    public const float L = 0.0065f; // temperature lapse rate [K/m]
    public const float R = 8.3145f; // universal gas constant [J/(mol K)]
    public const float M = 0.029f; // molar mass of air [kg/mol]
    public const float CdRectangle = 1.98f; // drag coefficient of flat square perpendicular to direction of flow
    public const float CdAirfoil = 0.04f; // drag coefficient of flat square perpendicular to direction of flow
    public const float CdSphere = 0.47f;
    public const float CdLongCylinder = 0.82f;
    public const float CdUprightCylinder = 1.16f;
    public const float CdLongCapsule = 0.20f;
    public const float CdUprightCapsule = 0.5f * (CdUprightCylinder + CdSphere);
    public const float CdCircle = 1.17f; // drag coefficient of flat cylinder perpendicular to direction of flow
    public const float seaLevelDensity = 1.2754f; // kg/m^3
    public const float rigidChipsAirDensity = 1.3f; // kg/m^3; = something heavier than sulphur hexafluoride
    public const float seaLevelHeliumDensity = 0.1786f; // kg/m^3
    public const float seawaterDensity = 1030f; // kg/m^3
    public const float waterToAirRatio = seawaterDensity / seaLevelDensity / 6.28f;
    public static float balloonConstant = 0.03048f;
    public static float balloonLiftConstant = 0.03048f / (-Physics.gravity.y);
    public static float volumeConstant = 4.18879f; // 4/3*pi
    public static float balloonDeltaRo = 7.8309f;
    public static float gIngame = 9.81f;
    public const float tireToAsphalt = 0.85f; // or 0.9 somewhere
    public const float rollingResistance = 0.005f; // or 0.01-0.001 somewhere
    public const float wheelRadius0 = 1f;
    public const float tinyMass = 0.25f * mediumMass;
    public const float smallMass = 0.5f * mediumMass;
    public const float mediumMass = 2.5f;
    public const float largeMass = 2f * mediumMass;
    public const float hugeMass = 4f * mediumMass;
}
