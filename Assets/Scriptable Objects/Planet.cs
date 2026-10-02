using System;
using UnityEngine;
using MissionCore;
using MissionGame;

[CreateAssetMenu(fileName = "Planet", menuName = "Scriptable Objects/Planet")]
public class Planet : ScriptableObject, ICelestialBody
{
    const double Deg2Rad = Math.PI / 180.0;
    const double SunMuKm3 = 1.32712440018e11;   // km³/s²
    public float rotationPeriodSeconds;
    [SerializeField] private SerializableVec3d _axis;
    public Vec3d Axis => _axis;

    public Planet parent;                        // null = orbits the Sun

    [Tooltip("Own μ in km³/s² (Earth 398600.4418). Needed for bodies that orbit THIS one.")]
    public double muKm3 = 0;
    public double Mu => muKm3 * 1e9;   // m³/s²
    public float radius = 0;                     // km

    [Header("Orbital elements (epoch J2000)")]
    public double semiMajorAxisKm = 149598023;   // a
    public double eccentricity = 0.0167086;      // e
    public double inclinationDeg = 0;            // i
    public double longAscNodeDeg = 0;            // Ω
    public double argPeriapsisDeg = 102.94719;   // ω
    public double meanAnomalyAtEpochDeg = 357.52911; // M0

    [Header("Rendering")]
    public Material material;

    KeplerOrbit orbit;

    // Built on first access; changes in the Inspector rebuild it
    public KeplerOrbit Orbit => orbit ??= BuildOrbit();

    KeplerOrbit BuildOrbit()
    {
        // μ of the CENTRAL BODY, not our own
        double centralMuKm3 = parent != null ? parent.muKm3 : SunMuKm3;
        if (centralMuKm3 <= 0)
            throw new InvalidOperationException($"{name}: parent {parent.name} has no μ set");

        return new KeplerOrbit(
            a:     semiMajorAxisKm * 1000.0,        // km → m
            ecc:   eccentricity,
            inc:   inclinationDeg * Deg2Rad,
            raan:  longAscNodeDeg * Deg2Rad,
            argPe: argPeriapsisDeg * Deg2Rad,
            m0:    meanAnomalyAtEpochDeg * Deg2Rad,
            epoch: 0,                               // t = seconds since J2000
            mu:    centralMuKm3 * 1e9);             // km³/s² → m³/s²
    }

    void OnValidate() => orbit = null;              // rebuild after a change in the Inspector

    // Position relative to the Sun in meters (recursively via parent)
    public Vec3d WorldPositionAt(double t)
    {
        Vec3d local = Orbit.PositionAt(t);
        return parent != null ? local + parent.WorldPositionAt(t) : local;
    }

    public Vec3d WorldVelocityAt(double t)
    {
        Vec3d local = Orbit.VelocityAt(t);
        return parent != null ? local + parent.WorldVelocityAt(t) : local;
    }
}