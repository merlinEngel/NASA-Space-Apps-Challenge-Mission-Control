using System;
using System.Collections.Generic;
using MissionCore.Bodies;
using MissionCore.Earth;
using MissionCore.Numerics;

namespace MissionCore.Simulation
{
    public class Forces
    {
        public static Vec3d Acceleration(Vec3d relPos, double t, ICelestialBody primary, IReadOnlyList<ICelestialBody> otherBodies)
        {
            return Acceleration(relPos, Vec3d.Zero, 1, t, primary, otherBodies);
        }

        public static Vec3d Acceleration(Vec3d relPos, Vec3d velocity, double massKg, double t,
                                 ICelestialBody primary, IReadOnlyList<ICelestialBody> otherBodies,
                                 EarthForceModel earth = default)
        {
            Vec3d a = -primary.Mu * relPos / Math.Pow(relPos.Length, 3);
            Vec3d primaryPos = primary.WorldPositionAt(t);

            foreach (ICelestialBody body in otherBodies)
            {
                Vec3d bodyToPrimary = body.WorldPositionAt(t) - primaryPos;
                Vec3d bodyToObject = bodyToPrimary - relPos;

                a += body.Mu * (bodyToObject / Math.Pow(bodyToObject.Length, 3) - bodyToPrimary / Math.Pow(bodyToPrimary.Length, 3));
            }

            if (earth.UseJ2) a += J2Drift.J2Acceleration(relPos);
            if (earth.DragAreaM2 > 0) a += Atmosphere.DragAcceleration(relPos, velocity, massKg, earth.DragAreaM2, earth.ActivityFactor);

            return a;
        }
    }

    // Which extra Earth forces act on a spacecraft. default = none (pure gravity, as before).
    public readonly struct EarthForceModel
    {
        public EarthForceModel(bool useJ2, double dragAreaM2, double activityFactor)
        {
            UseJ2 = useJ2;
            DragAreaM2 = dragAreaM2;
            ActivityFactor = activityFactor;
        }

        public bool UseJ2 { get; }
        public double DragAreaM2 { get; }        // 0 = no drag
        public double ActivityFactor { get; }    // solar activity, 1 = medium
    }
}