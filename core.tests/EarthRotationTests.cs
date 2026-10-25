using System;
using NUnit.Framework;
using MissionCore.Earth;
using MissionCore.Numerics;

namespace MissionCore.Tests
{
    // P5 step 7: Greenwich Mean Sidereal Time
    public class EarthRotationTests
    {
        const double Deg = Math.PI / 180;
        const double SiderealDayS = 86164.0905;
        static double Secs(int y, int mo, int d, int h = 0) =>
            (new DateTime(y, mo, d, h, 0, 0, DateTimeKind.Utc) - CoreConstants.J2000DateTime).TotalSeconds;

        [Test]
        public void Gmst_AtJ2000() =>
            Assert.That(EarthRotation.Gmst(0) / Deg, Is.EqualTo(280.4606).Within(1e-3));

        [Test]
        public void Gmst_At2027June1() =>
            Assert.That(EarthRotation.Gmst(Secs(2027, 6, 1)) / Deg, Is.EqualTo(249.255).Within(0.01));

        [Test]
        public void Gmst_IsRadiansBetweenZeroAndTwoPi()
        {
            for (double days = -9000; days <= 12000; days += 13.37)
            {
                double g = EarthRotation.Gmst(days * 86400);
                Assert.That(g, Is.GreaterThanOrEqualTo(0).And.LessThan(2 * Math.PI), $"day {days}");
            }
        }

        [Test]
        public void Gmst_AfterOneSiderealDay_SameAngle()
        {
            double t = Secs(2027, 6, 1);
            double diff = EarthRotation.Gmst(t + SiderealDayS) - EarthRotation.Gmst(t);
            diff = Math.IEEERemainder(diff, 2 * Math.PI);
            Assert.That(diff / Deg, Is.EqualTo(0).Within(0.001));
        }

        [Test]
        public void Gmst_AfterOneSolarDay_AboutOneDegreeMore()
        {
            double t = Secs(2027, 6, 1);
            double diff = Math.IEEERemainder(EarthRotation.Gmst(t + 86400) - EarthRotation.Gmst(t), 2 * Math.PI);
            Assert.That(diff / Deg, Is.EqualTo(0.9856).Within(0.001));
        }

        [Test]
        public void Gmst_RateMatchesEarthRotationConstant()
        {
            double t = Secs(2027, 6, 1, 3);
            double rate = Math.IEEERemainder(EarthRotation.Gmst(t + 60) - EarthRotation.Gmst(t), 2 * Math.PI) / 60;
            Assert.That(rate, Is.EqualTo(7.2921159e-5).Within(1e-9));
        }
    }
}
