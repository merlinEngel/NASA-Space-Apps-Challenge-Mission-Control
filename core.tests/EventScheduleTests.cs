using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 6: event schedule drawn from the seed at Start()
    public class EventScheduleTests
    {
        const double Day = 86400;

        static MissionSimulation Start(int seed, Action<Catalog> changeCatalog = null)
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            changeCatalog?.Invoke(catalog);
            return MissionSimulation.Start(catalog, MissionSimulationStartTests.TechDemo(), seed);
        }

        [Test]
        public void TechDemo_Seed42_OneSolarStormBetweenDay20And40()
        {
            MissionSimulation sim = Start(42);

            Assert.That(sim.EventSchedule, Has.Count.EqualTo(1));
            ScheduledEvent storm = sim.EventSchedule[0];
            Assert.That(storm.Type, Is.EqualTo(EventType.SolarStorm));
            Assert.That(storm.TimeS, Is.InRange(20 * Day, 40 * Day));
            Assert.That(storm.TimeS % 60, Is.EqualTo(0), "event must start on a tick");
        }

        [Test]
        public void SameSeed_SameSchedule()
        {
            Assert.That(Start(42).EventSchedule[0].TimeS, Is.EqualTo(Start(42).EventSchedule[0].TimeS));
        }

        [Test]
        public void ManySeeds_SpreadOverTheWholeRange()
        {
            var days = Enumerable.Range(1, 200).Select(seed => Start(seed).EventSchedule[0].TimeS / Day).ToList();

            Assert.That(days, Is.All.InRange(20.0, 40.0));
            Assert.That(days.Distinct().Count(), Is.GreaterThanOrEqualTo(150));
            Assert.That(days.Average(), Is.EqualTo(30).Within(2));   // uniform 20..40
            Assert.That(days.Min(), Is.LessThan(22));
            Assert.That(days.Max(), Is.GreaterThan(38));
        }

        [Test]
        public void MissionWithoutFixedEvents_EmptySchedule()
        {
            MissionSimulation sim = null;
            Assert.DoesNotThrow(() => sim = Start(42, c => c.Missions["techdemo"].fixedEvents = null));
            Assert.That(sim.EventSchedule, Is.Empty);
        }

        [Test]
        public void ValueRange_Sample_StaysInRangeAndCoversIt()
        {
            var range = new ValueRange(20, 40);
            var rng = new Random(1);
            var values = Enumerable.Range(0, 1000).Select(_ => range.Sample(rng)).ToList();

            Assert.That(values, Is.All.GreaterThanOrEqualTo(20.0).And.LessThan(40.0));
            Assert.That(values.Min(), Is.LessThan(21));
            Assert.That(values.Max(), Is.GreaterThan(39));
        }

        [Test]
        public void ValueRange_Sample_SameSeedSameValues()
        {
            var range = new ValueRange(20, 40);
            var a = new Random(7);
            var b = new Random(7);

            for (int i = 0; i < 10; i++)
                Assert.That(range.Sample(a), Is.EqualTo(range.Sample(b)));
        }

        [Test]
        public void Schedule_DoesNotChangeTheSimulationYet()
        {
            // The storm has no effect before P9, so the seed must not change anything else
            MissionSimulation a = Start(1);
            MissionSimulation b = Start(2);
            Assert.That(a.EventSchedule[0].TimeS, Is.Not.EqualTo(b.EventSchedule[0].TimeS));

            a.Step(Day);
            b.Step(Day);

            Assert.That(b.State.BatteryWh, Is.EqualTo(a.State.BatteryWh));
            Assert.That(b.State.DeliveredBits, Is.EqualTo(a.State.DeliveredBits));
            Assert.That(b.State.AltitudeM, Is.EqualTo(a.State.AltitudeM));
        }
    }
}
