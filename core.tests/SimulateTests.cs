using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 7: Simulate(), SimSummary and reproducibility (DoD B6)
    public class SimulateTests
    {
        const double Day = 86400;

        static MissionDesign Design(string file)
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string path = Path.Combine(root, "core.tests", "Data", "designs", file);
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), file);
        }

        static MissionSimulation Simulate(string file, int seed = 42) =>
            MissionSimulation.Simulate(MissionSimulationStartTests.RealCatalog(), Design(file), seed);

        [Test]
        public void Simulate_GoodDesign_RunsNinetyDays()
        {
            MissionSimulation sim = Simulate("techdemo_good.json");
            SimSummary s = SimSummary.From(sim);

            Assert.That(s.EndReason, Is.EqualTo(EndReason.DurationReached));
            Assert.That(s.DurationDays, Is.EqualTo(90));
            // 40 000 bit/s × 90 days = 38.88 GB measured, all but the last few minutes sent
            Assert.That(s.DeliveredBytes / 1e9, Is.EqualTo(38.86).Within(0.05));
            Assert.That(s.LostBytes, Is.EqualTo(0));
            Assert.That(s.FinalAltitudeM / 1e3, Is.EqualTo(491.1).Within(0.5));
            Assert.That(s.EnergyShortageWh, Is.EqualTo(0));
            Assert.That(s.MinBatterySoc, Is.GreaterThan(0.9));
            Assert.That(s.DeliveredBytes, Is.GreaterThanOrEqualTo(1e9), "mission goal: 1 GB delivered");
        }

        [Test]
        public void Summary_SameSeedTwice_IdenticalText()
        {
            string a = SimSummary.From(Simulate("low_power.json")).ToString();
            string b = SimSummary.From(Simulate("low_power.json")).ToString();

            Assert.That(b, Is.EqualTo(a));
        }

        [Test]
        public void Summary_GoodDesign_Text()
        {
            string text = SimSummary.From(Simulate("techdemo_good.json")).ToString();
            TestContext.Out.WriteLine(text);

            // GB = 1e9 bytes, invariant culture (decimal point), two decimals
            Assert.That(text, Does.StartWith("90 days, 38.86 GB delivered, 0.00 GB lost"));
            Assert.That(text, Does.Not.Contain("("), "no causes when nothing was lost");
        }

        [Test]
        public void Summary_LowPower_LosesToEnergyShortage()
        {
            SimSummary s = SimSummary.From(Simulate("low_power.json"));
            TestContext.Out.WriteLine(s.ToString());

            Assert.That(s.DeliveredBytes / 1e9, Is.EqualTo(20.6).Within(0.3));
            Assert.That(s.LostBytesByCause[LogCause.EnergyShortage] / 1e9, Is.EqualTo(18.2).Within(0.3));
            Assert.That(s.LostBytesByCause[LogCause.EnergyShortage], Is.EqualTo(s.LostBytes).Within(1), "values in bytes, like LostBytes");
            Assert.That(s.ToString(), Does.Contain("EnergyShortage"));
            Assert.That(s.ToString(), Does.Not.Contain("StorageFull"), "causes without loss are left out");
        }

        [Test]
        public void Summary_SlowDownlink_LosesToFullStorage()
        {
            SimSummary s = SimSummary.From(Simulate("slow_downlink.json"));
            string text = s.ToString();
            TestContext.Out.WriteLine(text);

            Assert.That(s.DeliveredBytes / 1e9, Is.EqualTo(0.32).Within(0.02));
            Assert.That(s.LostBytesByCause[LogCause.StorageFull] / 1e9, Is.EqualTo(22.56).Within(0.1));
            Assert.That(s.DeliveredBytes, Is.LessThan(1e9), "mission goal of 1 GB missed");
            Assert.That(text, Does.StartWith("90 days, 0.32 GB delivered, 22.56 GB lost"));
            Assert.That(text, Does.Contain("StorageFull 22.56 GB"));
        }

        // DoD B6: same seed with different dt sequences gives an identical log
        [Test]
        public void Simulate_And_StepWithRandomDt_GiveIdenticalLog()
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            MissionSimulation whole = MissionSimulation.Simulate(catalog, Design("low_power.json"), 42);

            MissionSimulation stepped = MissionSimulation.Start(catalog, Design("low_power.json"), 42);
            var rng = new Random(11);
            while (!stepped.State.Ended) stepped.Step(rng.Next(1, 100000));   // up to ~1660 ticks, below MaxStepsPerAdvance

            Assert.That(stepped.Log.Entries.Count, Is.EqualTo(whole.Log.Entries.Count));
            for (int i = 0; i < whole.Log.Entries.Count; i++)
            {
                SimLogEntry a = whole.Log.Entries[i], b = stepped.Log.Entries[i];
                Assert.That(b.StartS, Is.EqualTo(a.StartS), $"entry {i}");
                Assert.That(b.EndS, Is.EqualTo(a.EndS), $"entry {i}");
                Assert.That(b.Quantity, Is.EqualTo(a.Quantity), $"entry {i}");
                Assert.That(b.Cause, Is.EqualTo(a.Cause), $"entry {i}");
                Assert.That(b.Amount, Is.EqualTo(a.Amount), $"entry {i}");
            }

            Assert.That(stepped.Log.Samples.Count, Is.EqualTo(whole.Log.Samples.Count));
            for (int i = 0; i < whole.Log.Samples.Count; i++)
            {
                Assert.That(stepped.Log.Samples[i].BatterySoc, Is.EqualTo(whole.Log.Samples[i].BatterySoc), $"sample {i}");
                Assert.That(stepped.Log.Samples[i].StorageBits, Is.EqualTo(whole.Log.Samples[i].StorageBits), $"sample {i}");
            }
            Assert.That(SimSummary.From(stepped).ToString(), Is.EqualTo(SimSummary.From(whole).ToString()));
        }

        [Test]
        public void Simulate_TechDemo_UnderOneSecond()
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            MissionDesign design = Design("techdemo_good.json");
            MissionSimulation.Simulate(catalog, design, 1);   // warm-up (JIT)

            var watch = Stopwatch.StartNew();
            MissionSimulation.Simulate(catalog, design, 42);
            watch.Stop();
            TestContext.Out.WriteLine($"90 days: {watch.ElapsedMilliseconds} ms");

            Assert.That(watch.Elapsed.TotalSeconds, Is.LessThan(1));
        }
    }
}
