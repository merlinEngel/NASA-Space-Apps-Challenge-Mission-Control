using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MissionCore.Bodies;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Numerics;
using static MissionCore.Tests.TestHelpers;

namespace MissionCore.Tests
{
    // P5 steps 2 and 3: secular J2 drift, sun-synchronous inclination, drifting KeplerOrbit.
    // Reference values computed independently in Python (J2 = 1.08262668e-3).
    public class J2DriftTests
    {
        const double DegPerDay = 180 / Math.PI * 86400;   // rad/s -> deg/day

        static double A(double altitudeM) => REarth + altitudeM;

        // ---------- step 2: rates ----------

        [Test]
        public void NodalRate_IssLike_MinusFiveDegPerDay()
        {
            double rate = J2Drift.NodalRate(A(400e3), 0, 51.6 * Deg) * DegPerDay;
            Assert.That(rate, Is.EqualTo(-5.002).Within(0.005));
        }

        [Test]
        public void NodalRate_Sso550_FollowsTheSun()
        {
            double rate = J2Drift.NodalRate(A(550e3), 0, 97.6 * Deg) * DegPerDay;
            Assert.That(rate, Is.EqualTo(0.9866).Within(0.001));
            Assert.That(rate, Is.EqualTo(0.9856).Within(0.02));   // plan test: one turn per year
        }

        [Test]
        public void ApsidalRate_ZeroAtCriticalInclination()
        {
            double criticalDeg = Math.Acos(Math.Sqrt(0.2)) * 180 / Math.PI;   // 63.435°
            Assert.That(J2Drift.ApsidalRate(A(550e3), 0.01, criticalDeg * Deg), Is.EqualTo(0).Within(1e-15));
        }

        [Test]
        public void Equatorial_ApsidalIsMinusTwiceNodal()
        {
            double nodal = J2Drift.NodalRate(A(400e3), 0, 0) * DegPerDay;
            double apsidal = J2Drift.ApsidalRate(A(400e3), 0, 0) * DegPerDay;
            Assert.That(nodal, Is.EqualTo(-8.053).Within(0.005));
            Assert.That(apsidal, Is.EqualTo(16.107).Within(0.005));
            Assert.That(apsidal, Is.EqualTo(-2 * nodal).Within(1e-9));
        }

        [Test]
        public void NodalRate_MirrorsAt90Degrees()
        {
            double pro = J2Drift.NodalRate(A(700e3), 0, 30 * Deg);
            double retro = J2Drift.NodalRate(A(700e3), 0, 150 * Deg);
            Assert.That(retro, Is.EqualTo(-pro).Within(1e-18));
            Assert.That(J2Drift.NodalRate(A(700e3), 0, 90 * Deg), Is.EqualTo(0).Within(1e-20));
        }

        [Test]
        public void MeanAnomalyRate_ReferenceValues()
        {
            Assert.That(J2Drift.MeanAnomalyRate(A(400e3), 0, 51.6 * Deg) * DegPerDay, Is.EqualTo(0.634).Within(0.002));
            Assert.That(J2Drift.MeanAnomalyRate(A(550e3), 0, 97.6 * Deg) * DegPerDay, Is.EqualTo(-3.534).Within(0.002));
        }

        [Test]
        public void Rates_RejectInvalidOrbits()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => J2Drift.NodalRate(0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => J2Drift.NodalRate(A(400e3), 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => J2Drift.ApsidalRate(A(400e3), -0.1, 0));
        }

        // ---------- step 2: sun-synchronous inclination ----------

        [TestCase(400e3, 97.03)]
        [TestCase(500e3, 97.40)]
        [TestCase(550e3, 97.59)]
        [TestCase(705e3, 98.21)]   // Landsat 9 flies 98.2°
        [TestCase(800e3, 98.60)]
        public void SunSynchronousInclination_ReferenceValues(double altitudeM, double expectedDeg)
        {
            double? inc = J2Drift.SunSynchronousInclinationDeg(altitudeM);
            Assert.That(inc, Is.Not.Null);
            Assert.That(inc.Value, Is.EqualTo(expectedDeg).Within(0.01));
        }

        [Test]
        public void SunSynchronousInclination_NullAboveLimit()
        {
            Assert.That(J2Drift.SunSynchronousInclinationDeg(7000e3), Is.Null);
            Assert.That(J2Drift.SunSynchronousInclinationDeg(5900e3), Is.Not.Null);   // limit is ~5974 km
        }

        [Test]
        public void SunSynchronousInclination_GivesOneTurnPerTropicalYear()
        {
            double inc = J2Drift.SunSynchronousInclinationDeg(600e3).Value * Deg;
            double rate = J2Drift.NodalRate(A(600e3), 0, inc);
            Assert.That(rate, Is.EqualTo(2 * Math.PI / (Constants.TropicalYearDays * Constants.SecondsPerDay)).Within(1e-15));
        }

        [Test]
        public void OrbitPresets_SsoUsesComputedInclination()
        {
            string file = Path.Combine(StreamingDataDir(), "orbit_presets.json");
            List<OrbitPresetSpec> presets = CatalogLoader.LoadList<OrbitPresetSpec>(File.ReadAllText(file), "orbit_presets.json");

            OrbitPresetSpec sso = presets.Find(p => p.id == "sso");
            Assert.That(sso.EffectiveInclinationDeg, Is.EqualTo(97.40).Within(0.01));

            OrbitPresetSpec iss = presets.Find(p => p.id == "iss_like");
            Assert.That(iss.EffectiveInclinationDeg, Is.EqualTo(51.6));   // not sun-synchronous: JSON value
        }

        // ---------- step 3: drifting KeplerOrbit ----------

        static KeplerOrbit Sso550(double epoch = 0) =>
            J2Drift.ApplyTo(KeplerOrbit.Circular(550e3, 97.6 * Deg, 0.3, REarth, MuEarth, epoch));

        [Test]
        public void ApplyTo_SetsAllThreeRates()
        {
            var plain = KeplerOrbit.Circular(550e3, 97.6 * Deg, 0.3, REarth, MuEarth);
            var orbit = J2Drift.ApplyTo(plain);
            Assert.That(orbit.RaanRate, Is.EqualTo(J2Drift.NodalRate(plain.A, 0, plain.Inc)));
            Assert.That(orbit.ArgPeRate, Is.EqualTo(J2Drift.ApsidalRate(plain.A, 0, plain.Inc)));
            Assert.That(orbit.MeanAnomalyRate, Is.EqualTo(J2Drift.MeanAnomalyRate(plain.A, 0, plain.Inc)));
            Assert.That(orbit.A, Is.EqualTo(plain.A));
            Assert.That(orbit.RaanRad, Is.EqualTo(plain.RaanRad));
            Assert.That(plain.RaanRate, Is.EqualTo(0));   // original stays unchanged
        }

        [Test]
        public void ApplyTo_RejectsNonEarthOrbit()
        {
            var lunar = KeplerOrbit.Circular(100e3, 0, 0, 1737.4e3, 4.9048695e12);
            Assert.Throws<ArgumentException>(() => J2Drift.ApplyTo(lunar));
        }

        [Test]
        public void RaanAt_AdvancesOneDayFromEpoch()
        {
            double epoch = 865_080_000;
            var orbit = Sso550(epoch);
            double deltaDeg = (orbit.RaanAt(epoch + 86400) - orbit.RaanRad) * 180 / Math.PI;
            Assert.That(deltaDeg, Is.EqualTo(0.9866).Within(0.001));
            Assert.That(orbit.RaanAt(epoch), Is.EqualTo(orbit.RaanRad));
        }

        [Test]
        public void ZeroRates_StateMatchesFixedOrbit()
        {
            // Elliptic, inclined orbit with all angles set: compare with the closed-form position at periapsis
            var o = new KeplerOrbit(7e6, 0.1, 0.5, 1.0, 2.0, 0, 0, MuEarth);
            Vec3d p = o.PositionAt(0);   // M = 0 -> periapsis, r = a(1-e)
            double cO = Math.Cos(1.0), sO = Math.Sin(1.0), cw = Math.Cos(2.0), sw = Math.Sin(2.0), ci = Math.Cos(0.5), si = Math.Sin(0.5);
            var expected = new Vec3d(cO * cw - sO * sw * ci, sO * cw + cO * sw * ci, sw * si) * (7e6 * 0.9);
            AssertVec(expected, p, 1e-3);
        }

        [Test]
        public void OrbitPlane_RotatesAboutZ_ByNodalRate()
        {
            var orbit = Sso550();
            double t = 10 * 86400;
            var (r0, v0) = orbit.StateAt(0);
            var (r1, v1) = orbit.StateAt(t);
            Vec3d h0 = r0.Cross(v0), h1 = r1.Cross(v1);

            // node direction = z × h; its angle in the xy plane is RAAN + 90°
            double node0 = Math.Atan2(h0.x, -h0.y), node1 = Math.Atan2(h1.x, -h1.y);
            double turnedDeg = (node1 - node0) * 180 / Math.PI;
            Assert.That(turnedDeg, Is.EqualTo(orbit.RaanRate * t * 180 / Math.PI).Within(0.01));
            Assert.That(turnedDeg, Is.EqualTo(9.866).Within(0.01));
        }

        [Test]
        public void Inclination_StaysConstant()
        {
            var orbit = Sso550();
            var (r, v) = orbit.StateAt(10 * 86400);
            double incDeg = Vec3d.Angle(r.Cross(v), new Vec3d(0, 0, 1)) * 180 / Math.PI;
            Assert.That(incDeg, Is.EqualTo(97.6).Within(1e-6));
        }

        [Test]
        public void CircularOrbit_RadiusStaysConstant()
        {
            var orbit = Sso550();
            for (double t = 0; t <= 30 * 86400; t += 86400 / 7.0)
                Assert.That(orbit.PositionAt(t).Length, Is.EqualTo(REarth + 550e3).Within(1.0));
        }

        [Test]
        public void ArgumentOfLatitude_AdvancesWithAllRates()
        {
            // circular orbit: position angle in the plane is ω + M, so after one day it has moved by (n + ω̇ + Ṁ)·86400
            var orbit = Sso550();
            double expected = orbit.ArgPeAt(86400) + orbit.MeanAnomalyAt(86400);
            Vec3d r = orbit.PositionAt(86400);
            double raan = orbit.RaanAt(86400);
            var node = new Vec3d(Math.Cos(raan), Math.Sin(raan), 0);
            double u = Vec3d.Angle(node, r);
            if (r.z < 0) u = 2 * Math.PI - u;
            double diff = Math.IEEERemainder(u - expected, 2 * Math.PI);
            Assert.That(diff, Is.EqualTo(0).Within(1e-9));
        }
    }
}
