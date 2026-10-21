using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore.Bodies;
using MissionCore.Numerics;

namespace MissionCore.Earth
{
    public readonly struct OrbitPoint
    {
        public OrbitPoint(Vec3d positionM, bool inShadow)
        {
            PositionM = positionM;
            InShadow = inShadow;
        }

        public Vec3d PositionM { get; }
        public bool InShadow { get; }
        
    }

    public static class OrbitTrack
    {
        // One full revolution of the orbit, sampled at `count` equally spaced times from startS.
        // Rotated so that the shadow arc is one block at the end of the array.
        public static OrbitPoint[] Sample(KeplerOrbit orbit, double startS, int count, Vec3d sunDirection)
        {
            if (count <= 1) throw new ArgumentOutOfRangeException();

            double step = orbit.Period/count;

            List<OrbitPoint> points = new();

            for (int current = 0; current < count; current++)
            {
                Vec3d position = orbit.PositionAt(current*step + startS);
                points.Add(new(position, Shadow.IsInShadow(position, sunDirection)));
            }

            int startIndex = 0;

            for (int current = 1; current < count; current++)
            {
                if(points[current-1].InShadow && !points[current].InShadow) startIndex = current;
            }

            return points.Skip(startIndex).Concat(points.Take(startIndex)).ToArray();
        }
    }
}