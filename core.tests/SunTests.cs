using System;
using NUnit.Framework;
using MissionCore.Bodies;
using MissionCore.Earth;
using MissionCore.Numerics;

namespace MissionCore.Tests
{
    // P5 step 6: Sun direction (Astronomical Almanac low-precision formula)
    public class SunTests
    {
        const double Deg = Math.PI / 180;
        static double Secs(int y, int mo, int d, int h = 0) =>
            (new DateTime(y, mo, d, h, 0, 0, DateTimeKind.Utc) - Constants.J2000DateTime).TotalSeconds;

        static double RightAscensionDeg(Vec3d v) => (Math.Atan2(v.y, v.x) / Deg + 360) % 360;
        static double DeclinationDeg(Vec3d v) => Math.Asin(v.z) / Deg;

        [Test]
        public void Direction_AtJ2000()
        {
            // Almanac: RA 281.3°, dec -23.0° on 2000-01-01 12:00 UTC
            Vec3d s = Sun.Direction(0);
            Assert.That(RightAscensionDeg(s), Is.EqualTo(281.29).Within(0.05));
            Assert.That(DeclinationDeg(s), Is.EqualTo(-23.03).Within(0.05));
        }

        [Test]
        public void Direction_AtMarchEquinox2027_PointsToVernalEquinox()
        {
            Vec3d s = Sun.Direction(Secs(2027, 3, 20, 15));
            Assert.That(Vec3d.Angle(s, Vec3d.UnitX) / Deg, Is.LessThan(0.3));
        }

        [Test]
        public void Direction_AtJuneSolstice2027_DeclinationIsObliquity()
        {
            Vec3d s = Sun.Direction(Secs(2027, 6, 21, 12));
            Assert.That(DeclinationDeg(s), Is.EqualTo(23.435).Within(0.02));
            Assert.That(RightAscensionDeg(s), Is.EqualTo(89.9).Within(0.2));
        }

        [Test]
        public void Direction_IsUnitVector()
        {
            for (double days = -3000; days <= 12000; days += 37.3)
                Assert.That(Sun.Direction(days * 86400).Length, Is.EqualTo(1).Within(1e-12), $"day {days}");
        }

        [Test]
        public void Direction_MovesAboutOneDegreePerDay()
        {
            double t = Secs(2027, 6, 1);
            double angle = Vec3d.Angle(Sun.Direction(t), Sun.Direction(t + 86400)) / Deg;
            Assert.That(angle, Is.EqualTo(0.96).Within(0.05));
        }

        [Test]
        public void DistanceAu_PerihelionAndAphelion()
        {
            Assert.That(Sun.DistanceAu(0), Is.EqualTo(0.9833).Within(0.0005));                    // early January
            Assert.That(Sun.DistanceAu(Secs(2027, 7, 4)), Is.EqualTo(1.0167).Within(0.0005));     // early July
        }
    }
}
