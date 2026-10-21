using System;
using NUnit.Framework;
using MissionCore.Earth;

namespace MissionCore.Tests
{
    // P5 step 5: orbit decay by air drag and orbital lifetime
    public class OrbitDecayTests
    {
        const double Day = 86400;

        // 3U CubeSat: 4 kg, 0.03 m² mean cross-section (values checked with Python, 1 km midpoint steps)
        [TestCase(1.0, 187.5)]
        [TestCase(0.5, 374.9)]   // low solar activity: thinner air, twice as long
        [TestCase(3.0, 62.5)]    // high solar activity
        public void LifetimeS_CubeSat3UAt400Km(double activityFactor, double expectedDays)
        {
            double days = OrbitDecay.LifetimeS(400e3, 4, 0.03, activityFactor) / Day;
            Assert.That(days, Is.EqualTo(expectedDays).Within(2).Percent);
        }

        // Tech demo: dry mass + propellant 15.3 kg, drag area 0.102 + 0.2 * 0.5 m²
        [TestCase(300e3, 13.3)]
        [TestCase(400e3, 106.5)]
        [TestCase(500e3, 641.7)]
        public void LifetimeS_TechDemoAltitudes(double altitudeM, double expectedDays)
        {
            double days = OrbitDecay.LifetimeS(altitudeM, 15.3, 0.202) / Day;
            Assert.That(days, Is.EqualTo(expectedDays).Within(2).Percent);
        }

        [Test]
        public void LifetimeS_DoubleMass_DoubleLifetime()
        {
            double one = OrbitDecay.LifetimeS(400e3, 4, 0.03);
            double two = OrbitDecay.LifetimeS(400e3, 8, 0.03);
            Assert.That(two / one, Is.EqualTo(2).Within(0.1).Percent);
        }

        [Test]
        public void LifetimeS_HigherIsLonger()
        {
            double previous = 0;
            for (double h = 200e3; h <= 700e3; h += 50e3)
            {
                double life = OrbitDecay.LifetimeS(h, 4, 0.03);
                Assert.That(life, Is.GreaterThan(previous), $"{h / 1e3} km");
                previous = life;
            }
        }

        [TestCase(150e3)]
        [TestCase(100e3)]
        [TestCase(0)]
        public void LifetimeS_AtOrBelowReentryAltitude_IsZero(double altitudeM)
        {
            Assert.That(OrbitDecay.LifetimeS(altitudeM, 4, 0.03), Is.EqualTo(0));
        }

        [Test]
        public void LifetimeS_HighOrbit_IsCappedAt100Years()
        {
            Assert.That(OrbitDecay.LifetimeS(1000e3, 4, 0.03), Is.EqualTo(OrbitDecay.MaxLifetimeS));
            Assert.That(OrbitDecay.MaxLifetimeS, Is.EqualTo(100 * 365.25 * Day));
        }

        [Test]
        public void LifetimeS_NoDragArea_IsCapped()
        {
            // No area means no drag: the satellite never comes down
            Assert.That(OrbitDecay.LifetimeS(400e3, 4, 0), Is.EqualTo(OrbitDecay.MaxLifetimeS));
        }

        [TestCase(0, 0.03)]
        [TestCase(-1, 0.03)]
        [TestCase(4, -0.01)]
        public void LifetimeS_InvalidInput_Throws(double massKg, double areaM2)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OrbitDecay.LifetimeS(400e3, massKg, areaM2));
        }

        [Test]
        public void DecayRate_ReferenceValue()
        {
            // B = Cd * A / m = 2.2 * 0.03 / 4
            double b = OrbitDecay.DragCoefficient * 0.03 / 4;
            Assert.That(OrbitDecay.DecayRate(400e3, b), Is.EqualTo(-3.1947e-3).Within(0.5).Percent);
        }

        [Test]
        public void DecayRate_IsNegativeAndStrongerLowerDown()
        {
            double b = 0.0165;
            double previous = 0;
            for (double h = 800e3; h >= 200e3; h -= 50e3)
            {
                double rate = OrbitDecay.DecayRate(h, b);
                Assert.That(rate, Is.LessThan(0), $"{h / 1e3} km");
                Assert.That(rate, Is.LessThan(previous), $"{h / 1e3} km");
                previous = rate;
            }
        }

        [Test]
        public void DecayRate_ScalesWithActivityFactor()
        {
            double b = 0.0165;
            Assert.That(OrbitDecay.DecayRate(400e3, b, 3.0), Is.EqualTo(3 * OrbitDecay.DecayRate(400e3, b)).Within(1e-9).Percent);
        }
    }
}
