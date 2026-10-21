using System;
using System.Collections.Generic;
using NUnit.Framework;
using static MissionCore.Tests.TestHelpers;
using MissionCore.Bodies;
using MissionCore.Earth;
using MissionCore.Numerics;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P5 step 8: J2 and air drag as forces for the numerically integrated satellite
    public class EarthForcesTests
    {
        static readonly TestBody Earth = new TestBody(MuEarth);
        static readonly IReadOnlyList<ICelestialBody> None = new List<ICelestialBody>();
        const double Omega = 7.2921159e-5;

        // 3U CubeSat from step 5: 4 kg, 0.03 m² mean cross-section
        const double CubeSatMassKg = 4, CubeSatAreaM2 = 0.03;

        static SpacecraftState Circular(double altitudeM, double incDeg, double massKg = CubeSatMassKg)
        {
            var orbit = KeplerOrbit.Circular(altitudeM, incDeg * Deg, 0, REarth, MuEarth, 0);
            var (r, v) = orbit.StateAt(0);
            return new SpacecraftState(r, v, massKg, massKg);
        }

        static SpacecraftState Propagate(SpacecraftState s, double durationS, EarthForceModel model)
        {
            const double dt = 10;
            for (double t = 0; t < durationS - 1e-9; t += dt)
                s = Integrator.RK4Step(s, t, dt, Earth, None, model);
            return s;
        }

        static double Raan(SpacecraftState s)
        {
            Vec3d h = s.relPos.Cross(s.velocity);
            return Math.Atan2(h.x, -h.y);
        }

        static double SemiMajorAxis(SpacecraftState s) =>
            1 / (2 / s.relPos.Length - s.velocity.SqrLength / MuEarth);

        // ---------- J2Acceleration ----------

        [Test]
        public void J2_AboveEquator_PointsInward()
        {
            double r = REarth + 400e3;
            Vec3d a = J2Drift.J2Acceleration(new Vec3d(r, 0, 0));
            Assert.That(a.x, Is.EqualTo(-0.012475).Within(0.1).Percent);
            Assert.That(Math.Abs(a.y) + Math.Abs(a.z), Is.LessThan(1e-15));
        }

        [Test]
        public void J2_AbovePole_PointsOutwardTwiceAsStrong()
        {
            double r = REarth + 400e3;
            Vec3d equator = J2Drift.J2Acceleration(new Vec3d(r, 0, 0));
            Vec3d pole = J2Drift.J2Acceleration(new Vec3d(0, 0, r));
            Assert.That(pole.z, Is.GreaterThan(0));
            Assert.That(pole.z / equator.Length, Is.EqualTo(2).Within(1e-9));
        }

        [Test]
        public void J2_IsMirrorSymmetricToEquator()
        {
            var p = new Vec3d(4e6, -3e6, 4.5e6);
            Vec3d up = J2Drift.J2Acceleration(p);
            Vec3d down = J2Drift.J2Acceleration(new Vec3d(p.x, p.y, -p.z));
            AssertVec(new Vec3d(up.x, up.y, -up.z), down, 1e-15);
        }

        // ---------- DragAcceleration ----------

        [Test]
        public void Drag_PolarOrbit400Km_ReferenceValue()
        {
            var s = Circular(400e3, 90);
            Vec3d a = Atmosphere.DragAcceleration(s.relPos, s.velocity, CubeSatMassKg, CubeSatAreaM2);
            Assert.That(a.Length, Is.EqualTo(1.807e-6).Within(1).Percent);
            Assert.That(Vec3d.Angle(a, -s.velocity) / Deg, Is.LessThan(5));   // against the motion (air turns a little sideways)
        }

        [Test]
        public void Drag_NoArea_IsZero()
        {
            var s = Circular(400e3, 51.6);
            Assert.That(Atmosphere.DragAcceleration(s.relPos, s.velocity, CubeSatMassKg, 0), Is.EqualTo(Vec3d.Zero));
        }

        [Test]
        public void Drag_MovingWithTheAir_IsZero()
        {
            var pos = new Vec3d(REarth + 400e3, 0, 0);
            var withAir = new Vec3d(-Omega * pos.y, Omega * pos.x, 0);
            Assert.That(Atmosphere.DragAcceleration(pos, withAir, CubeSatMassKg, CubeSatAreaM2).Length, Is.LessThan(1e-20));
        }

        [Test]
        public void Drag_DoubleMass_HalfAcceleration()
        {
            var s = Circular(400e3, 51.6);
            double one = Atmosphere.DragAcceleration(s.relPos, s.velocity, 4, CubeSatAreaM2).Length;
            double two = Atmosphere.DragAcceleration(s.relPos, s.velocity, 8, CubeSatAreaM2).Length;
            Assert.That(one / two, Is.EqualTo(2).Within(1e-9));
        }

        // ---------- Forces ----------

        [Test]
        public void Forces_DefaultModel_SameAsOldOverload()
        {
            var s = Circular(400e3, 51.6);
            Vec3d oldWay = Forces.Acceleration(s.relPos, 0, Earth, None);
            Vec3d newWay = Forces.Acceleration(s.relPos, s.velocity, s.mass, 0, Earth, None, default);
            AssertVec(oldWay, newWay, 1e-15);
        }

        [Test]
        public void Forces_WithJ2AndDrag_AddsBoth()
        {
            var s = Circular(400e3, 51.6);
            Vec3d pure = Forces.Acceleration(s.relPos, 0, Earth, None);
            Vec3d full = Forces.Acceleration(s.relPos, s.velocity, s.mass, 0, Earth, None, new EarthForceModel(true, CubeSatAreaM2, 1));
            Vec3d expected = pure + J2Drift.J2Acceleration(s.relPos)
                           + Atmosphere.DragAcceleration(s.relPos, s.velocity, s.mass, CubeSatAreaM2, 1);
            AssertVec(expected, full, 1e-15);
        }

        // ---------- Integrated over days (Python RK4 reference, 10 s steps) ----------

        [Test]
        public void Test5_SsoRaanDriftsAboutOneDegreePerDay()
        {
            // satellit-numerisch-erklaert.md, test 5: formula 0.987°/day, numerically 0.992°/day
            var start = Circular(550e3, 97.6);
            var end = Propagate(start, 5 * 86400, new EarthForceModel(true, 0, 1));
            double perDay = (Raan(end) - Raan(start)) / Deg / 5;
            Assert.That(perDay, Is.EqualTo(0.99).Within(0.02));
        }

        [Test]
        public void IssOrbit_RaanDriftsMinusFiveDegreesPerDay()
        {
            var start = Circular(400e3, 51.6);
            var end = Propagate(start, 86400, new EarthForceModel(true, 0, 1));
            Assert.That((Raan(end) - Raan(start)) / Deg, Is.EqualTo(-5.00).Within(0.03));
        }

        [Test]
        public void J2Only_DoesNotChangeTheSemiMajorAxisOnAverage()
        {
            // J2 is conservative: the orbit wobbles by a few km but does not sink
            var start = Circular(400e3, 51.6);
            var end = Propagate(start, 86400, new EarthForceModel(true, 0, 1));
            Assert.That(Math.Abs(SemiMajorAxis(end) - SemiMajorAxis(start)), Is.LessThan(15e3));
            Assert.That(end.relPos.Length - REarth, Is.InRange(380e3, 420e3));
        }

        [TestCase(90, 277)]     // matches OrbitDecay from step 5 (276 m/day)
        [TestCase(51.6, 255)]   // less: the co-rotating air takes away some of the head wind
        public void DragOnly_SemiMajorAxisDropsPerDay(double incDeg, double expectedM)
        {
            var start = Circular(400e3, incDeg);
            var end = Propagate(start, 86400, new EarthForceModel(false, CubeSatAreaM2, 1));
            Assert.That(SemiMajorAxis(start) - SemiMajorAxis(end), Is.EqualTo(expectedM).Within(3).Percent);
        }

        [Test]
        public void DragOnly_DoubleMass_HalfTheLoss()
        {
            var model = new EarthForceModel(false, CubeSatAreaM2, 1);
            var light = Circular(400e3, 90, 4);
            var heavy = Circular(400e3, 90, 8);
            double lossLight = SemiMajorAxis(light) - SemiMajorAxis(Propagate(light, 86400, model));
            double lossHeavy = SemiMajorAxis(heavy) - SemiMajorAxis(Propagate(heavy, 86400, model));
            Assert.That(lossLight / lossHeavy, Is.EqualTo(2).Within(2).Percent);
        }
    }
}
