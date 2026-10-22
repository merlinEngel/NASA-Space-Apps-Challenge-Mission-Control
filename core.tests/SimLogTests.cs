using NUnit.Framework;
using MissionCore.Simulation;

namespace MissionCore.Tests
{
    // P6 step 1: protocol stored as episodes instead of one entry per tick
    public class SimLogTests
    {
        const double Tick = 60;

        [Test]
        public void Add_ConsecutiveTicksSameCause_ExtendOneEpisode()
        {
            var log = new SimLog();
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.StorageFull, 100);
            log.Add(60, Tick, LogQuantity.DataBits, LogCause.StorageFull, 50);

            Assert.That(log.Entries, Has.Count.EqualTo(1));
            SimLogEntry e = log.Entries[0];
            Assert.That(e.StartS, Is.EqualTo(0));
            Assert.That(e.EndS, Is.EqualTo(120));
            Assert.That(e.Amount, Is.EqualTo(150));
            Assert.That(e.Quantity, Is.EqualTo(LogQuantity.DataBits));
            Assert.That(e.Cause, Is.EqualTo(LogCause.StorageFull));
        }

        [Test]
        public void Add_GapBetweenTicks_StartsNewEpisode()
        {
            var log = new SimLog();
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.StorageFull, 100);
            log.Add(120, Tick, LogQuantity.DataBits, LogCause.StorageFull, 100);

            Assert.That(log.Entries, Has.Count.EqualTo(2));
            Assert.That(log.Entries[1].StartS, Is.EqualTo(120));
            Assert.That(log.Entries[0].EndS, Is.EqualTo(60));
        }

        [Test]
        public void Add_AlternatingCauses_EachCauseKeepsItsOwnEpisode()
        {
            var log = new SimLog();
            // Both causes active in the same ticks: each continues its own episode
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.EnergyShortage, 10);
            log.Add(0, Tick, LogQuantity.EnergyWh, LogCause.EnergyShortage, 1);
            log.Add(60, Tick, LogQuantity.DataBits, LogCause.EnergyShortage, 10);
            log.Add(60, Tick, LogQuantity.EnergyWh, LogCause.EnergyShortage, 1);
            // Then a different cause for the same quantity
            log.Add(120, Tick, LogQuantity.DataBits, LogCause.SafeMode, 5);

            Assert.That(log.Entries, Has.Count.EqualTo(3));
            Assert.That(log.Total(LogQuantity.DataBits, LogCause.EnergyShortage), Is.EqualTo(20));
            Assert.That(log.Total(LogQuantity.EnergyWh, LogCause.EnergyShortage), Is.EqualTo(2));
            Assert.That(log.Total(LogQuantity.DataBits, LogCause.SafeMode), Is.EqualTo(5));
        }

        [Test]
        public void Add_CauseInterruptedByOtherCause_StartsNewEpisode()
        {
            var log = new SimLog();
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.StorageFull, 1);
            log.Add(60, Tick, LogQuantity.DataBits, LogCause.SafeMode, 1);
            log.Add(120, Tick, LogQuantity.DataBits, LogCause.StorageFull, 1);

            Assert.That(log.Entries, Has.Count.EqualTo(3));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Add_ZeroOrNegativeAmount_IsIgnored(double amount)
        {
            var log = new SimLog();
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.StorageFull, amount);

            Assert.That(log.Entries, Is.Empty);
            Assert.That(log.Total(LogQuantity.DataBits, LogCause.StorageFull), Is.EqualTo(0));
        }

        [Test]
        public void Total_SumsAllEpisodesOfOneCause()
        {
            var log = new SimLog();
            log.Add(0, Tick, LogQuantity.DataBits, LogCause.StorageFull, 100);
            log.Add(60, Tick, LogQuantity.DataBits, LogCause.StorageFull, 100);
            log.Add(600, Tick, LogQuantity.DataBits, LogCause.StorageFull, 30);

            Assert.That(log.Entries, Has.Count.EqualTo(2));
            Assert.That(log.Total(LogQuantity.DataBits, LogCause.StorageFull), Is.EqualTo(230));
            Assert.That(log.Total(LogQuantity.DataBits, LogCause.EnergyShortage), Is.EqualTo(0));
        }

        [Test]
        public void AddSample_KeepsOrder()
        {
            var log = new SimLog();
            log.AddSample(new SimSample(0, 1, 0, 500e3));
            log.AddSample(new SimSample(600, 0.9, 1e6, 499.9e3));

            Assert.That(log.Samples, Has.Count.EqualTo(2));
            Assert.That(log.Samples[1].TimeS, Is.EqualTo(600));
            Assert.That(log.Samples[1].BatterySoc, Is.EqualTo(0.9));
        }
    }
}
