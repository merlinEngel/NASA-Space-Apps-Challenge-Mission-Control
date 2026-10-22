using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 2: tick loop with orbit, shadow and ground contact
    public class MissionSimulationStepTests
    {
        const double Day = 86400;
        const int TicksPerDay = 1440;

        static MissionSimulation StartTechDemo(Action<MissionDesign> change = null, Action<Catalog> changeCatalog = null)
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            MissionDesign design = MissionSimulationStartTests.TechDemo();
            change?.Invoke(design);
            changeCatalog?.Invoke(catalog);
            return MissionSimulation.Start(catalog, design, 42);
        }

        // Runs one day tick by tick and counts the ticks in shadow and in contact.
        static (int shadow, int contact) RunDayCounting(MissionSimulation sim)
        {
            int shadow = 0, contact = 0;
            for (int i = 0; i < TicksPerDay; i++)
            {
                Assert.That(sim.Step(60), Is.EqualTo(1));
                if (sim.State.InShadow) shadow++;
                if (sim.State.InContact) contact++;
            }
            return (shadow, contact);
        }

        [Test]
        public void Step_LessThanOneTick_DoesNothing()
        {
            MissionSimulation sim = StartTechDemo();

            Assert.That(sim.Step(30), Is.EqualTo(0));
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(0));
        }

        [Test]
        public void Step_TwoHalfTicks_RunOneTick()
        {
            MissionSimulation sim = StartTechDemo();

            sim.Step(30);
            Assert.That(sim.Step(30), Is.EqualTo(1));   // the first 30 s were kept in the accumulator
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(60));
        }

        [Test]
        public void Step_OneDay_Runs1440Ticks()
        {
            MissionSimulation sim = StartTechDemo();

            Assert.That(sim.Step(Day), Is.EqualTo(TicksPerDay));
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(Day));
            Assert.That(sim.Clock.StepIndex, Is.EqualTo(TicksPerDay));
        }

        [Test]
        public void Step_DifferentDtSequences_GiveIdenticalResult()
        {
            MissionSimulation once = StartTechDemo();
            once.Step(Day);

            MissionSimulation perTick = StartTechDemo();
            for (int i = 0; i < TicksPerDay; i++) perTick.Step(60);

            // Uneven chunks like varying frame times; whole seconds so the sum is exact
            MissionSimulation chunks = StartTechDemo();
            var rng = new Random(1);
            double done = 0;
            while (done < Day)
            {
                double dt = Math.Min(rng.Next(1, 300), Day - done);
                chunks.Step(dt);
                done += dt;
            }

            foreach (MissionSimulation other in new[] { perTick, chunks })
            {
                Assert.That(other.Clock.StepIndex, Is.EqualTo(TicksPerDay));
                TestHelpers.AssertVec(once.State.PositionM, other.State.PositionM, 1e-3);
                Assert.That(other.State.InShadow, Is.EqualTo(once.State.InShadow));
                Assert.That(other.State.InContact, Is.EqualTo(once.State.InContact));
                Assert.That(other.Log.Samples.Count, Is.EqualTo(once.Log.Samples.Count));
                for (int i = 0; i < once.Log.Samples.Count; i++)
                    Assert.That(other.Log.Samples[i].TimeS, Is.EqualTo(once.Log.Samples[i].TimeS));
            }
        }

        [Test]
        public void Step_AfterMissionEnded_DoesNothing()
        {
            MissionSimulation sim = StartTechDemo();
            sim.State.EndReason = EndReason.Reentry;   // set directly until step 5 can end a mission

            Assert.That(sim.Step(600), Is.EqualTo(0));
            Assert.That(sim.State.MissionTimeS, Is.EqualTo(0));
        }

        // Updated in step 5: the altitude now decays a few cm per tick, so the radius follows State.AltitudeM
        [Test]
        public void Orbit_RadiusFollowsAltitude()
        {
            MissionSimulation sim = StartTechDemo();

            for (int i = 0; i < 200; i++)
            {
                double altitudeBefore = sim.State.AltitudeM;   // the tick positions the satellite at this altitude
                sim.Step(60);
                Assert.That(sim.State.PositionM.Length, Is.EqualTo(TestHelpers.REarth + altitudeBefore).Within(1e-3));
                Assert.That(sim.State.AltitudeM, Is.EqualTo(500e3).Within(20));
            }
        }

        [Test]
        public void Orbit_SatelliteMovesAlongTheOrbit()
        {
            MissionSimulation sim = StartTechDemo();
            Vec3d start = sim.State.PositionM;

            sim.Step(24 * 60);

            // The position belongs to the start of the last tick: 23 ticks = 1380 s after the epoch.
            // Circular orbit at 500 km: n = sqrt(mu / r³), J2 adds a little (< 0.1° here).
            double r = TestHelpers.REarth + 500e3;
            double expectedDeg = Math.Sqrt(TestHelpers.MuEarth / (r * r * r)) * 1380 / TestHelpers.Deg;
            double actualDeg = Vec3d.Angle(start, sim.State.PositionM) / TestHelpers.Deg;
            Assert.That(actualDeg, Is.EqualTo(expectedDeg).Within(0.5));   // about 87.5°
        }

        [Test]
        public void Shadow_TechDemoDay_MatchesAnalyticFraction()
        {
            MissionSimulation sim = StartTechDemo();
            (int shadow, _) = RunDayCounting(sim);

            // Beta angle in the middle of the day, with the J2-drifted node (SSO: drift follows the Sun)
            double tMid = sim.Parameters.EpochS + Day / 2;
            double inc = 97.5 * TestHelpers.Deg;
            double raan = J2Drift.NodalRate(TestHelpers.REarth + 500e3, 0, inc) * Day / 2;
            double beta = Shadow.BetaAngle(inc, raan, Sun.Direction(tMid));
            double expected = Shadow.EclipseFraction(500e3, beta);

            Assert.That(shadow / (double)TicksPerDay, Is.EqualTo(expected).Within(0.02));
            // P5 check in Python: about 16 % for this orbit on 01.06.2027
            Assert.That(expected, Is.EqualTo(0.16).Within(0.03));
        }

        // DoD B6: shadow per orbit from the simulation, averaged over a day with a fixed Sun direction,
        // against the analytic value for 400 km and beta 0° (±3 % relative).
        [Test]
        public void Shadow_400km_Beta0_MatchesAnalyticWithin3Percent()
        {
            // Equatorial orbit at the March equinox: the Sun lies in the orbit plane, beta ≈ 0°
            // (declination changes by only 0.4° per day around the equinox)
            MissionSimulation sim = StartTechDemo(
                d => { d.AltitudeM = 400e3; d.InclinationDeg = 0; },
                c => c.Missions["techdemo"].epochUtc = new DateTime(2027, 3, 20, 12, 0, 0, DateTimeKind.Utc));
            (int shadow, _) = RunDayCounting(sim);

            double expected = Shadow.EclipseFraction(400e3, 0);
            Assert.That(shadow / (double)TicksPerDay, Is.EqualTo(expected).Within(expected * 0.03));
        }

        [Test]
        public void Contact_TechDemoDay_MatchesGroundContactScan()
        {
            MissionSimulation sim = StartTechDemo();
            (_, int contact) = RunDayCounting(sim);

            ContactStats scan = GroundContact.Scan(500e3, 97.5, sim.Parameters.Stations,
                sim.Parameters.EpochS, sim.Parameters.RaanRad, days: 1);
            double simulatedS = contact * 60.0;

            // 60 s ticks miss up to a minute at each end of a pass; ~18 passes a day → ±10 %
            Assert.That(simulatedS, Is.EqualTo(scan.ContactTimeSPerDay).Within(scan.ContactTimeSPerDay * 0.10));
            // P5 check in Python: about 7 770 s per day (3-day mean)
            Assert.That(simulatedS, Is.EqualTo(7770).Within(1000));
        }

        [Test]
        public void Contact_WithoutTransmitter_NeverInContact()
        {
            MissionSimulation sim = StartTechDemo(d => d.CommsId = null);
            (_, int contact) = RunDayCounting(sim);

            Assert.That(contact, Is.EqualTo(0));
        }

        [Test]
        public void Samples_EveryTenTicks_FirstAt600s()
        {
            MissionSimulation sim = StartTechDemo();
            sim.Step(Day);

            Assert.That(sim.Log.Samples, Has.Count.EqualTo(144));
            Assert.That(sim.Log.Samples[0].TimeS, Is.EqualTo(600));
            Assert.That(sim.Log.Samples[143].TimeS, Is.EqualTo(Day));
            Assert.That(sim.Log.Samples[0].AltitudeM, Is.EqualTo(500e3).Within(1));   // step 5: ~6 cm decay per tick
            Assert.That(sim.Log.Samples[0].BatterySoc, Is.InRange(0, 1));
        }
    }
}
