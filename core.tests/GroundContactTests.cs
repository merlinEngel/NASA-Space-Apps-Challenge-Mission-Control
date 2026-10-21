using System.Collections.Generic;
using NUnit.Framework;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Numerics;

namespace MissionCore.Tests
{
    public class GroundContactTests
    {
        static GroundStationSpec Station(double latDeg, double lonDeg = 0, double minElevationDeg = 5) => new GroundStationSpec
        {
            id = "st", name = "Station", latDeg = latDeg, lonDeg = lonDeg, minElevationDeg = minElevationDeg, bands = new[] { "S" }
        };

        [Test]
        public void ElevationDeg_SatelliteStraightAbove_Is90()
        {
            var satellite = new Vec3d(Constants.RadiusEarth + 500000, 0, 0);
            Assert.That(GroundContact.ElevationDeg(satellite, 0, 0, 0), Is.EqualTo(90).Within(1e-9));
        }

        [Test]
        public void ElevationDeg_SatelliteOnOtherSide_IsNegative()
        {
            var satellite = new Vec3d(-(Constants.RadiusEarth + 500000), 0, 0);
            Assert.That(GroundContact.ElevationDeg(satellite, 0, 0, 0), Is.LessThan(0));
        }


        // Relative period to an equatorial station = 2π / (n - ωEarth) ≈ 101 min, so about 14 passes per day.
        [Test]
        public void EquatorialOrbit_EquatorialStation_HasContactEveryOrbit()
        {
            var stats = GroundContact.Scan(500000, 0, new List<GroundStationSpec> { Station(0) });
            Assert.That(stats.ContactsPerDay, Is.InRange(13.0, 15.5));
            Assert.That(stats.LongestGapS, Is.LessThan(2 * 3600));
            Assert.That(stats.ContactTimeSPerDay, Is.GreaterThan(0));
        }

        [Test]
        public void EquatorialOrbit_HighLatitudeStation_HasNoContact()
        {
            var stats = GroundContact.Scan(500000, 0, new List<GroundStationSpec> { Station(80) });
            Assert.That(stats.ContactsPerDay, Is.EqualTo(0));
            Assert.That(stats.ContactTimeSPerDay, Is.EqualTo(0));
            Assert.That(stats.LongestGapS, Is.EqualTo(GroundContact.DefaultDays * Constants.SecondsPerDay).Within(GroundContact.DefaultStepS));
        }

        [Test]
        public void PolarOrbit_PolarStation_HasManyContacts()
        {
            var stats = GroundContact.Scan(500000, 90, new List<GroundStationSpec> { Station(89) });
            Assert.That(stats.ContactsPerDay, Is.GreaterThanOrEqualTo(12));
        }

        [Test]
        public void HigherMinElevation_GivesLessContactTime()
        {
            var low = GroundContact.Scan(500000, 51.6, new List<GroundStationSpec> { Station(48, 11, 5) });
            var high = GroundContact.Scan(500000, 51.6, new List<GroundStationSpec> { Station(48, 11, 30) });
            Assert.That(high.ContactTimeSPerDay, Is.LessThan(low.ContactTimeSPerDay));
        }

        [Test]
        public void TwoStations_CountPassesOfBoth_ButNotDoubleContactTime()
        {
            var one = GroundContact.Scan(500000, 0, new List<GroundStationSpec> { Station(0) });
            var twice = GroundContact.Scan(500000, 0, new List<GroundStationSpec> { Station(0), Station(0) });
            Assert.That(twice.ContactsPerDay, Is.EqualTo(2 * one.ContactsPerDay).Within(1e-9));
            Assert.That(twice.ContactTimeSPerDay, Is.EqualTo(one.ContactTimeSPerDay).Within(1e-9));
        }

        [Test]
        public void NoStationsOrNoAltitude_GivesNoContact()
        {
            Assert.That(GroundContact.Scan(500000, 0, new List<GroundStationSpec>()).ContactsPerDay, Is.EqualTo(0));
            Assert.That(GroundContact.Scan(500000, 0, null).ContactsPerDay, Is.EqualTo(0));
            Assert.That(GroundContact.Scan(0, 0, new List<GroundStationSpec> { Station(0) }).ContactsPerDay, Is.EqualTo(0));
        }

        // ---------- P5 step 7: real Earth rotation (GMST) and J2-drifting orbit ----------

        static readonly double TechDemoEpoch = (new System.DateTime(2027, 6, 1, 0, 0, 0, System.DateTimeKind.Utc) - Constants.J2000DateTime).TotalSeconds;
        static GroundStationSpec Svalbard => Station(78.23, 15.40, 5);
        static GroundStationSpec Weilheim => Station(47.88, 11.08, 5);

        [Test]
        public void ElevationDeg_UsesEarthRotationAngle()
        {
            // Station at 30° E on the equator, Earth turned by 100°: it faces inertial longitude 130°
            double angle = 130 * System.Math.PI / 180;
            var satellite = new Vec3d(System.Math.Cos(angle), System.Math.Sin(angle), 0) * (Constants.RadiusEarth + 500000);
            Assert.That(GroundContact.ElevationDeg(satellite, 0, 30, 100 * System.Math.PI / 180), Is.EqualTo(90).Within(1e-4));
            // 1e-4 instead of 1e-6: asin is very steep at 90°, so rounding in the last digit already moves the result by 1e-6°
        }

        // Reference values: Python, epoch 2027-06-01 00:00 UTC, raan 0, 3 days, 10 s steps, with GMST and J2
        [Test]
        public void Scan_TechDemoSso_ContactTimeMatchesReference()
        {
            var stats = GroundContact.Scan(500000, 97.5, new List<GroundStationSpec> { Svalbard, Weilheim }, TechDemoEpoch, 0);
            Assert.That(stats.ContactTimeSPerDay, Is.EqualTo(7767).Within(5).Percent);
            Assert.That(stats.ContactsPerDay, Is.EqualTo(18.0).Within(10).Percent);
        }

        [Test]
        public void Scan_IssWeilheim_ContactTimeMatchesReference()
        {
            var stats = GroundContact.Scan(400000, 51.6, new List<GroundStationSpec> { Weilheim }, TechDemoEpoch, 0);
            Assert.That(stats.ContactTimeSPerDay, Is.EqualTo(2420).Within(5).Percent);
        }

        [Test]
        public void Scan_RealDate_StaysWithin5PercentOfOldModel()
        {
            // Old model (fixed Earth start, no J2): 7717 s per day for the tech demo
            var stats = GroundContact.Scan(500000, 97.5, new List<GroundStationSpec> { Svalbard, Weilheim }, TechDemoEpoch, 0);
            Assert.That(stats.ContactTimeSPerDay, Is.EqualTo(7717).Within(5).Percent);
        }

        [Test]
        public void Scan_OtherRaan_SimilarContactTime()
        {
            var stations = new List<GroundStationSpec> { Weilheim };
            var a = GroundContact.Scan(500000, 97.5, stations, TechDemoEpoch, 0);
            var b = GroundContact.Scan(500000, 97.5, stations, TechDemoEpoch, System.Math.PI / 2);
            Assert.That(b.ContactTimeSPerDay, Is.EqualTo(a.ContactTimeSPerDay).Within(10).Percent);
        }

        [Test]
        public void Scan_DifferentStartTime_ChangesPassTimesButNotAverage()
        {
            var stations = new List<GroundStationSpec> { Station(0) };
            var a = GroundContact.Scan(500000, 0, stations, TechDemoEpoch, 0);
            var b = GroundContact.Scan(500000, 0, stations, TechDemoEpoch + 6 * 3600, 0);
            Assert.That(b.ContactsPerDay, Is.EqualTo(a.ContactsPerDay).Within(1));
        }
    }
}
