using System;
using System.Collections.Generic;

namespace MissionCore
{
    public class Forces
    {
        public static Vec3d Acceleration(Vec3d relPos, double t, ICelestialBody primary, IReadOnlyList<ICelestialBody> otherBodies)
        {
            Vec3d a = -primary.Mu * relPos / Math.Pow(relPos.Length, 3);
            Vec3d primaryPos = primary.WorldPositionAt(t);

            foreach (ICelestialBody body in otherBodies)
            {
                Vec3d bodyToPrimary = body.WorldPositionAt(t) - primaryPos;
                Vec3d bodyToObject = bodyToPrimary - relPos;

                a += body.Mu * (bodyToObject / Math.Pow(bodyToObject.Length, 3) - bodyToPrimary / Math.Pow(bodyToPrimary.Length, 3));
            }

            return a;
        }
    }
}