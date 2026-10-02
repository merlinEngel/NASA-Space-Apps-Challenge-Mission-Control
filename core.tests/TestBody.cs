using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;
using static MissionCore.Tests.TestHelpers;

namespace MissionCore.Tests
{
    // A celestial body for tests only: fixed position, freely chosen μ
    sealed class TestBody : ICelestialBody
    {
        public double Mu { get; }
        readonly Vec3d position;
        public TestBody(double mu, Vec3d position = default) { Mu = mu; this.position = position; }
        public Vec3d WorldPositionAt(double t) => position;
    }

    public class IntegratorTests
    {
        static readonly TestBody Earth = new TestBody(MuEarth);
        static readonly IReadOnlyList<ICelestialBody> None = new List<ICelestialBody>();

        const double A400 = REarth + 400e3;                  // 6 778 137 m
        static readonly double V400 = Math.Sqrt(MuEarth / A400);
        static readonly double T400 = 2 * Math.PI * Math.Sqrt(A400 * A400 * A400 / MuEarth);

        static SpacecraftState Start400(double mass = 100, double dryMass = 50) =>
            new SpacecraftState(new Vec3d(A400, 0, 0), new Vec3d(0, V400, 0), mass, dryMass);

        // Propagates exactly 'duration' seconds; the last step is shortened to the remainder
        static SpacecraftState Propagate(SpacecraftState s, double duration, double dt, ICelestialBody primary)
        {
            double t = 0;
            while (t < duration - 1e-9)
            {
                double h = Math.Min(dt, duration - t);
                s = Integrator.RK4Step(s, t, h, primary, None);
                t += h;
            }
            return s;
        }

        static double Energy(SpacecraftState s) =>
            s.velocity.SqrLength / 2 - MuEarth / s.relPos.Length;

        [Test]
        public void OneStep_NumericExampleFromGuide()
        {
            // rk4-erklaert.md, section 5 (verified with Python)
            var s = Integrator.RK4Step(Start400(), 0, 10, Earth, None);
            AssertVec(new Vec3d(6777703.207077, 76683.945807, 0), s.relPos, 1e-3);
            AssertVec(new Vec3d(-86.757659, 7668.067397, 0), s.velocity, 1e-5);
        }

        [Test]
        public void WithoutForce_MovesInStraightLine()
        {
            var noGravity = new TestBody(0);
            var start = new SpacecraftState(new Vec3d(1, 2, 3), new Vec3d(10, -20, 5), 50, 50);
            var s = Integrator.RK4Step(start, 0, 7, noGravity, None);
            AssertVec(new Vec3d(71, -138, 38), s.relPos, 1e-9);   // r + 7·v
            AssertVec(start.velocity, s.velocity, 1e-12);
        }

        [Test]
        public void Mass_StaysSameWithoutEngine()
        {
            var s = Integrator.RK4Step(Start400(123.4, 56.7), 0, 10, Earth, None);
            Assert.That(s.mass, Is.EqualTo(123.4));
            Assert.That(s.dryMass, Is.EqualTo(56.7));   // RK4 must pass dryMass through, otherwise the fuel check breaks
        }

        [Test]
        public void OneOrbit_BackAtStart()
        {
            var s = Propagate(Start400(), T400, 10, Earth);
            AssertVec(new Vec3d(A400, 0, 0), s.relPos, 0.1);     // measured: 0.017 m
            AssertVec(new Vec3d(0, V400, 0), s.velocity, 1e-4);
        }

        [Test]
        public void ErrorOrder4_HalfStepSize_AboutSixteenTimesMoreAccurate()
        {
            var ziel = new Vec3d(A400, 0, 0);
            double fehler20 = Vec3d.Distance(ziel, Propagate(Start400(), T400, 20, Earth).relPos);
            double fehler10 = Vec3d.Distance(ziel, Propagate(Start400(), T400, 10, Earth).relPos);
            // measured: 0.289 m vs. 0.017 m, ratio 17
            Assert.That(fehler20 / fehler10, Is.InRange(10.0, 25.0));
        }

        [Test]
        public void TenOrbits_EnergyIsConserved()
        {
            var start = Start400();
            var s = Propagate(start, 10 * T400, 10, Earth);
            double e0 = Energy(start);
            Assert.That(Math.Abs((Energy(s) - e0) / e0), Is.LessThan(1e-8));   // measured: 3e-10
        }

        [Test]
        public void MatchesKepler()
        {
            // Inclined eccentric orbit: start state from Kepler, then compare numerically against Kepler
            var orbit = new KeplerOrbit(9000e3, 0.2, 40 * Deg, 70 * Deg, 110 * Deg, 0.5, 0, MuEarth);
            var (r0, v0) = orbit.StateAt(0);
            var s = Propagate(new SpacecraftState(r0, v0, 100, 50), 3 * 3600, 10, Earth);
            AssertVec(orbit.PositionAt(3 * 3600), s.relPos, 1.0);
        }

        [Test]
        public void EmptyBodyList_ChangesNothing()
        {
            var mitMondOhneMasse = new List<ICelestialBody> { new TestBody(0, new Vec3d(384e6, 0, 0)) };
            var a = Integrator.RK4Step(Start400(), 0, 10, Earth, None);
            var b = Integrator.RK4Step(Start400(), 0, 10, Earth, mitMondOhneMasse);
            AssertVec(a.relPos, b.relPos, 1e-9);
        }

        [Test]
        public void SameSequence_SameResult()
        {
            var a = Propagate(Start400(), 5000, 10, Earth);
            var b = Propagate(Start400(), 5000, 10, Earth);
            Assert.That(a.relPos.x, Is.EqualTo(b.relPos.x));   // bit-identical, no tolerance
            Assert.That(a.relPos.y, Is.EqualTo(b.relPos.y));
        }
    }
}
