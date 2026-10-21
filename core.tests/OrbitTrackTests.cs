using System;
using System.Linq;
using NUnit.Framework;
using static MissionCore.Tests.TestHelpers;
using MissionCore.Bodies;
using MissionCore.Budgets;
using MissionCore.Earth;
using MissionCore.Numerics;
using MissionCore.Time;

namespace MissionCore.Tests
{
    // P5 step 9: points for the orbit preview (shadow arc) and ground station markers
    public class OrbitTrackTests
    {
        const int Count = 180;

        static KeplerOrbit Circular(double altitudeM, double incDeg, double raanDeg = 0) =>
            KeplerOrbit.Circular(altitudeM, incDeg * Deg, raanDeg * Deg, REarth, MuEarth, 0);

        static double DarkFraction(OrbitPoint[] points) => points.Count(p => p.InShadow) / (double)points.Length;

        // ---------- StationPosition ----------

        [Test]
        public void StationPosition_EquatorGreenwich_IsOnXAxis() =>
            AssertVec(new Vec3d(REarth, 0, 0), GroundContact.StationPosition(0, 0, 0), 1e-6);

        [Test]
        public void StationPosition_NorthPole_IsOnZAxis() =>
            AssertVec(new Vec3d(0, 0, REarth), GroundContact.StationPosition(90, 0, 0), 1e-6);

        [Test]
        public void StationPosition_EarthRotationActsLikeLongitude()
        {
            AssertVec(new Vec3d(0, REarth, 0), GroundContact.StationPosition(0, 0, Math.PI / 2), 1e-6);
            AssertVec(GroundContact.StationPosition(48, 101, 0), GroundContact.StationPosition(48, 11, 90 * Deg), 1e-6);
        }

        [Test]
        public void ElevationDeg_StationDirectlyBelow_Is90()
        {
            Vec3d station = GroundContact.StationPosition(47.9, 11.1, 0.3);
            Vec3d above = station * ((REarth + 500e3) / REarth);
            Assert.That(GroundContact.ElevationDeg(above, 47.9, 11.1, 0.3), Is.EqualTo(90).Within(1e-4));   // asin is very steep at 90°
        }

        // ---------- Sample ----------

        [Test]
        public void Sample_GivesCountPointsOnTheOrbitRadius()
        {
            var points = OrbitTrack.Sample(Circular(400e3, 51.6), 0, Count, new Vec3d(1, 0, 0));
            Assert.That(points.Length, Is.EqualTo(Count));
            foreach (var p in points)
                Assert.That(p.PositionM.Length, Is.EqualTo(REarth + 400e3).Within(1));
        }

        [Test]
        public void Sample_Equator400Km_SunInPlane_DarkFraction()
        {
            // beta = 0: acos(sqrt(h² + 2Rh) / r) / π = 0.3901 (Python, step 6 formula)
            var points = OrbitTrack.Sample(Circular(400e3, 0), 0, Count, new Vec3d(1, 0, 0));
            Assert.That(DarkFraction(points), Is.EqualTo(0.3901).Within(1.0 / Count));
        }

        [Test]
        public void Sample_Equator500Km_Sun60DegreesAbovePlane_DarkFraction()
        {
            // beta = 60°: 0.2307 (Python)
            var sun = new Vec3d(Math.Cos(60 * Deg), 0, Math.Sin(60 * Deg));
            var points = OrbitTrack.Sample(Circular(500e3, 0), 0, Count, sun);
            Assert.That(DarkFraction(points), Is.EqualTo(0.2307).Within(1.0 / Count));
        }

        [Test]
        public void Sample_SunAlongOrbitNormal_NoShadow()
        {
            // i = 90°, Ω = 90° → orbit normal (sin i sin Ω, −sin i cos Ω, cos i) = (1, 0, 0)
            var points = OrbitTrack.Sample(Circular(400e3, 90, 90), 0, Count, new Vec3d(1, 0, 0));
            Assert.That(points.Any(p => p.InShadow), Is.False);
        }

        [TestCase(0.0)]
        [TestCase(1234.5)]
        [TestCase(4000.0)]
        public void Sample_ShadowArcIsOneBlockAtTheEnd(double startS)
        {
            var points = OrbitTrack.Sample(Circular(400e3, 51.6, 30), startS, Count, new Vec3d(0.6, 0.8, 0));
            AssertShadowAtTheEnd(points);
        }

        [Test]
        public void Sample_ShadowEndsBetweenTheLastTwoSamples_StillRotated()
        {
            // Equator orbit, 2° per sample. The sun is placed so the satellite leaves the shadow at 357°
            // after the start point: sample 178 (356°) is dark, sample 179 (358°) is lit.
            // Shadow half-width at 400 km, beta 0: 0.3901 · 180° = 70.2°, so the sun is at 357° − 180° − 70.2° = 106.8°.
            KeplerOrbit orbit = Circular(400e3, 0);
            Vec3d start = orbit.PositionAt(0);
            double sunAngle = Math.Atan2(start.y, start.x) + 106.8 * Deg;
            var sun = new Vec3d(Math.Cos(sunAngle), Math.Sin(sunAngle), 0);

            var points = OrbitTrack.Sample(orbit, 0, Count, sun);
            AssertShadowAtTheEnd(points);
        }

        [TestCase(400e3, 51.6, 0)]
        [TestCase(550e3, 97.6, 200)]
        public void Sample_MatchesEclipseFractionFormula(double altitudeM, double incDeg, double raanDeg)
        {
            double t = SimTimeConverter.ToSeconds(new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc));
            Vec3d sun = Sun.Direction(t);
            var orbit = KeplerOrbit.Circular(altitudeM, incDeg * Deg, raanDeg * Deg, REarth, MuEarth, t);
            double beta = Shadow.BetaAngle(incDeg * Deg, raanDeg * Deg, sun);

            var points = OrbitTrack.Sample(orbit, t, Count, sun);
            Assert.That(DarkFraction(points), Is.EqualTo(Shadow.EclipseFraction(altitudeM, beta)).Within(1.5 / Count));
        }

        static void AssertShadowAtTheEnd(OrbitPoint[] points)
        {
            int firstDark = Array.FindIndex(points, p => p.InShadow);
            Assert.That(firstDark, Is.GreaterThan(0), "point 0 must be lit and there must be a shadow");
            Assert.That(points[^1].InShadow, Is.True, "last point must be dark");
            for (int i = firstDark; i < points.Length; i++)
                Assert.That(points[i].InShadow, Is.True, $"point {i} lies inside the dark block but is lit");
        }
    }
}
