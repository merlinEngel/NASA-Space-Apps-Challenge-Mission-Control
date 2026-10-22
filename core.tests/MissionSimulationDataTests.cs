using System;
using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 4: data flow (storage, downlink, losses with cause)
    public class MissionSimulationDataTests
    {
        const double Day = 86400;
        const int TicksPerDay = 1440;
        const double Tick = 60;

        // camera_wide: 2 Mbit/s × 2 % duty cycle = 40 000 bit/s → 2.4 Mbit per tick
        const double BitsPerTick = 40000 * Tick;
        // S-band: 1 Mbit/s → 60 Mbit per tick
        const double DownlinkPerTick = 1e6 * Tick;

        static MissionDesign Design(string file)
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string path = Path.Combine(root, "core.tests", "Data", "designs", file);
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(path), file);
        }

        static MissionSimulation Start(MissionDesign design, Action<Catalog> changeCatalog = null)
        {
            Catalog catalog = MissionSimulationStartTests.RealCatalog();
            changeCatalog?.Invoke(catalog);
            return MissionSimulation.Start(catalog, design, 42);
        }

        static double LostTotal(MissionSimulation sim) =>
            sim.Log.Total(LogQuantity.DataBits, LogCause.EnergyShortage) +
            sim.Log.Total(LogQuantity.DataBits, LogCause.StorageFull) +
            sim.Log.Total(LogQuantity.DataBits, LogCause.SafeMode);

        // Every bit the instruments could have measured is sent, stored or lost, and the log agrees with LostBits.
        static void AssertBalance(MissionSimulation sim)
        {
            double possible = sim.Parameters.InstrumentDataBps * sim.State.MissionTimeS;
            double accounted = sim.State.DeliveredBits + sim.State.StorageBits + sim.State.LostBits;
            Assert.That(accounted, Is.EqualTo(possible).Within(1), $"at t = {sim.State.MissionTimeS} s");
            Assert.That(LostTotal(sim), Is.EqualTo(sim.State.LostBits).Within(1), "log and LostBits disagree");
            Assert.That(sim.State.StorageBits, Is.InRange(0, sim.State.StorageCapacityBits));
        }

        [Test]
        public void Data_TickWithoutContact_FillsStorage()
        {
            MissionSimulation sim = Start(Design("techdemo_good.json"));

            for (int i = 0; i < 200; i++)
            {
                double before = sim.State.StorageBits;
                sim.Step(Tick);
                if (!sim.State.InContact)
                {
                    Assert.That(sim.State.StorageBits - before, Is.EqualTo(BitsPerTick).Within(1e-6));
                    return;
                }
            }
            Assert.Fail("no tick without contact found");
        }

        [Test]
        public void Data_TickWithContact_SendsUpToDownlinkRate()
        {
            MissionSimulation sim = Start(Design("techdemo_good.json"));

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.State.StorageBits = 100e6;   // 100 Mbit waiting before every tick
                double deliveredBefore = sim.State.DeliveredBits;
                sim.Step(Tick);
                if (sim.State.InContact)
                {
                    // 100 + 2.4 − 60 = 42.4 Mbit
                    Assert.That(sim.State.StorageBits, Is.EqualTo(100e6 + BitsPerTick - DownlinkPerTick).Within(1e-6));
                    Assert.That(sim.State.DeliveredBits - deliveredBefore, Is.EqualTo(DownlinkPerTick).Within(1e-6));
                    return;
                }
            }
            Assert.Fail("no contact within a day");
        }

        [Test]
        public void Data_ContactWithAlmostEmptyStorage_SendsEverythingButNeverGoesNegative()
        {
            MissionSimulation sim = Start(Design("techdemo_good.json"));

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.State.StorageBits = 1e6;
                double deliveredBefore = sim.State.DeliveredBits;
                sim.Step(Tick);
                if (sim.State.InContact)
                {
                    Assert.That(sim.State.StorageBits, Is.EqualTo(0));
                    Assert.That(sim.State.DeliveredBits - deliveredBefore, Is.EqualTo(1e6 + BitsPerTick).Within(1e-6));
                    return;
                }
            }
            Assert.Fail("no contact within a day");
        }

        [Test]
        public void Data_GoodDesignOneDay_NothingLostAndBalanced()
        {
            MissionSimulation sim = Start(Design("techdemo_good.json"));

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.Step(Tick);
                AssertBalance(sim);
            }

            // 40 000 bit/s × 86 400 s = 3.456 Gbit measured; downlink (~7.8 Gbit/day) keeps up
            Assert.That(sim.State.LostBits, Is.EqualTo(0));
            Assert.That(sim.State.DeliveredBits + sim.State.StorageBits, Is.EqualTo(3.456e9).Within(1));
            Assert.That(sim.State.DeliveredBits, Is.GreaterThan(2.5e9));
        }

        [Test]
        public void Data_CommandedSafeMode_LosesEverythingAsSafeMode()
        {
            MissionSimulation sim = Start(Design("techdemo_good.json"));
            sim.Command(true);

            sim.Step(Day);

            Assert.That(sim.Log.Total(LogQuantity.DataBits, LogCause.SafeMode), Is.EqualTo(3.456e9).Within(1));
            Assert.That(sim.Log.Total(LogQuantity.DataBits, LogCause.EnergyShortage), Is.EqualTo(0));
            Assert.That(sim.State.LostBits, Is.EqualTo(3.456e9).Within(1));
            Assert.That(sim.State.DeliveredBits, Is.EqualTo(0));
            AssertBalance(sim);
        }

        [Test]
        public void Data_LowPowerDesign_LosesDataToEnergyShortage()
        {
            MissionSimulation sim = Start(Design("low_power.json"));

            for (int i = 0; i < TicksPerDay; i++)
            {
                sim.Step(Tick);
                AssertBalance(sim);
            }

            Assert.That(sim.Log.Total(LogQuantity.DataBits, LogCause.EnergyShortage), Is.GreaterThan(0));
            Assert.That(sim.Log.Total(LogQuantity.DataBits, LogCause.SafeMode), Is.EqualTo(0));
        }

        [Test]
        public void Data_SmallStorageWithoutTransmitter_OverflowsAsStorageFull()
        {
            MissionDesign design = Design("techdemo_good.json");
            design.CommsId = null;
            MissionSimulation sim = Start(design, c => c.Platforms[design.PlatformId].storageBits = 10e6);

            sim.Step(4 * Tick);
            Assert.That(sim.State.StorageBits, Is.EqualTo(4 * BitsPerTick).Within(1e-6));   // 9.6 Mbit, not full yet
            Assert.That(sim.State.LostBits, Is.EqualTo(0));

            sim.Step(Tick);                                                                   // 12 Mbit → 2 Mbit too much
            Assert.That(sim.State.StorageBits, Is.EqualTo(10e6).Within(1e-6));
            Assert.That(sim.State.LostBits, Is.EqualTo(5 * BitsPerTick - 10e6).Within(1e-6));

            sim.Step(10 * Tick);                                                              // every further tick loses 2.4 Mbit
            Assert.That(sim.State.StorageBits, Is.EqualTo(10e6).Within(1e-6));
            Assert.That(sim.Log.Total(LogQuantity.DataBits, LogCause.StorageFull), Is.EqualTo(15 * BitsPerTick - 10e6).Within(1e-6));
            Assert.That(sim.Log.Entries, Has.Count.EqualTo(1), "consecutive overflow ticks form one episode");
            AssertBalance(sim);
        }

        [Test]
        public void Data_DifferentDtSequences_GiveSameData()
        {
            MissionSimulation once = Start(Design("low_power.json"));
            once.Step(Day);

            MissionSimulation chunks = Start(Design("low_power.json"));
            var rng = new Random(5);
            double done = 0;
            while (done < Day)
            {
                double dt = Math.Min(rng.Next(1, 500), Day - done);
                chunks.Step(dt);
                done += dt;
            }

            Assert.That(chunks.State.DeliveredBits, Is.EqualTo(once.State.DeliveredBits));
            Assert.That(chunks.State.LostBits, Is.EqualTo(once.State.LostBits));
            Assert.That(chunks.State.StorageBits, Is.EqualTo(once.State.StorageBits));
        }
    }
}
