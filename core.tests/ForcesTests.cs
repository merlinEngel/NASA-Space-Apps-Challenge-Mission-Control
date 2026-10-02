using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;
using static MissionCore.Tests.TestHelpers;

namespace MissionCore.Tests
{
    public class ForcesTests
    {
        static readonly TestBody Earth = new TestBody(MuEarth);
        static readonly IReadOnlyList<ICelestialBody> None = new List<ICelestialBody>();

        [Test]
        public void CentralForce_Magnitude_IsMuOverRSquared()
        {
            double r = REarth + 400e3;
            var a = Forces.Acceleration(new Vec3d(r, 0, 0), 0, Earth, None);
            Assert.That(a.Length, Is.EqualTo(MuEarth / (r * r)).Within(1e-9));
        }

        [Test]
        public void CentralForce_PointsToCenter()
        {
            var pos = new Vec3d(1e7, -2e7, 3e6);
            var a = Forces.Acceleration(pos, 0, Earth, None);
            AssertVec(-pos.Normalized, a.Normalized, 1e-12);
        }

        [Test]
        public void CentralForce_DoubleDistance_QuarterAcceleration()
        {
            double r = 8e6;
            double a1 = Forces.Acceleration(new Vec3d(r, 0, 0), 0, Earth, None).Length;
            double a2 = Forces.Acceleration(new Vec3d(0, 2 * r, 0), 0, Earth, None).Length;
            Assert.That(a1 / a2, Is.EqualTo(4.0).Within(1e-9));
        }

        [Test]
        public void TidalTerm_MasslessBody_ChangesNothing()
        {
            var pos = new Vec3d(7e6, 1e6, 0);
            var mondOhneMasse = new List<ICelestialBody> { new TestBody(0, new Vec3d(384e6, 0, 0)) };
            AssertVec(Forces.Acceleration(pos, 0, Earth, None),
                      Forces.Acceleration(pos, 0, Earth, mondOhneMasse), 1e-15);
        }

        [Test]
        public void TidalTerm_AtCenterOfPrimary_IsZero()
        {
            // Direct and indirect terms cancel when relPos = 0 (the center falls freely along)
            var sonne = new List<ICelestialBody> { new TestBody(MuSun, new Vec3d(AU, 0, 0)) };
            var gezeiten = Forces.Acceleration(new Vec3d(1, 0, 0), 0, new TestBody(0), sonne);
            Assert.That(gezeiten.Length, Is.LessThan(1e-9));
        }
    }
}
