using System.Collections.Generic;
using NUnit.Framework;

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

        [Test]
        public void SatellitePosition_KeepsRadiusAndReachesInclinationLatitude()
        {
            double r = Constants.RadiusEarth + 500000;
            Vec3d top = GroundContact.SatellitePosition(r, 51.6 * System.Math.PI / 180, System.Math.PI / 2);
            Assert.That(top.Length, Is.EqualTo(r).Within(1e-6));
            Assert.That(System.Math.Asin(top.z / r) * 180 / System.Math.PI, Is.EqualTo(51.6).Within(1e-9));
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
    }
}
