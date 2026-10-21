using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Time;

namespace MissionCore.Tests
{
    // P5 step 1: date <-> seconds since J2000, Julian date, mission epoch
    public class SimTimeConverterTests
    {
        static readonly DateTime Oct12_2026 = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void ToSeconds_J2000IsZero()
        {
            Assert.That(SimTimeConverter.ToSeconds(Constants.J2000DateTime), Is.EqualTo(0));
        }

        [Test]
        public void ToSeconds_ReferenceDate()
        {
            Assert.That(SimTimeConverter.ToSeconds(Oct12_2026), Is.EqualTo(845_035_200).Within(1e-6));
        }

        [Test]
        public void ToSeconds_UsesTotalSeconds_NotSecondsComponent()
        {
            // One day and 30 s after J2000: TimeSpan.Seconds would be 30, TotalSeconds is 86430
            var t = Constants.J2000DateTime.AddDays(1).AddSeconds(30);
            Assert.That(SimTimeConverter.ToSeconds(t), Is.EqualTo(86_430).Within(1e-9));
        }

        [Test]
        public void ToSeconds_LocalTime_SameAsUtc()
        {
            DateTime local = Oct12_2026.ToLocalTime();
            Assert.That(local.Kind, Is.EqualTo(DateTimeKind.Local));
            Assert.That(SimTimeConverter.ToSeconds(local), Is.EqualTo(SimTimeConverter.ToSeconds(Oct12_2026)).Within(1e-6));
        }

        [Test]
        public void ToUtc_RoundTrip()
        {
            var x = new DateTime(2027, 6, 1, 13, 45, 17, DateTimeKind.Utc);
            DateTime back = SimTimeConverter.ToUtc(SimTimeConverter.ToSeconds(x));
            Assert.That(Math.Abs((back - x).TotalSeconds), Is.LessThan(1e-3));
        }

        [Test]
        public void JulianDate_ReferenceValues()
        {
            Assert.That(SimTimeConverter.JulianDate(0), Is.EqualTo(2_451_545.0));
            Assert.That(SimTimeConverter.JulianDate(SimTimeConverter.ToSeconds(Oct12_2026)), Is.EqualTo(2_461_325.5).Within(1e-9));
        }

        [Test]
        public void JulianCenturies_OneCenturyAfterJ2000()
        {
            Assert.That(SimTimeConverter.JulianCenturies(36525 * Constants.SecondsPerDay), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(SimTimeConverter.JulianCenturies(0), Is.EqualTo(0));
        }

        [Test]
        public void TechDemo_EpochSeconds()
        {
            string file = Path.Combine(TestHelpers.StreamingDataDir(), "missions.json");
            List<MissionTemplate> missions = CatalogLoader.LoadList<MissionTemplate>(File.ReadAllText(file), "missions.json");
            MissionTemplate techdemo = missions.Find(m => m.id == "techdemo");
            Assert.That(techdemo, Is.Not.Null);
            Assert.That(techdemo.EpochSeconds, Is.EqualTo(865_080_000).Within(1e-6));   // 2027-06-01 00:00 UTC
        }
    }
}
