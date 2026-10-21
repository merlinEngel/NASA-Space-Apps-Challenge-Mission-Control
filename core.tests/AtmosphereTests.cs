using System.IO;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Rules;

namespace MissionCore.Tests
{
    // P5 step 4: exponential atmosphere (Vallado table 8-4) and solar activity factor
    public class AtmosphereTests
    {
        [TestCase(0, 1.225)]
        [TestCase(400e3, 3.725e-12)]
        [TestCase(425e3, 2.430e-12)]   // middle of a band
        [TestCase(550e3, 3.183e-13)]
        [TestCase(200e3, 2.789e-10)]
        public void Density_ReferenceValues(double altitudeM, double expected)
        {
            Assert.That(Atmosphere.Density(altitudeM), Is.EqualTo(expected).Within(0.5).Percent);
        }

        [Test]
        public void Bands_AreSortedAndStartAtZero()
        {
            Assert.That(Atmosphere.Bands[0].BaseAltitudeM, Is.EqualTo(0));
            for (int i = 1; i < Atmosphere.Bands.Length; i++)
                Assert.That(Atmosphere.Bands[i].BaseAltitudeM, Is.GreaterThan(Atmosphere.Bands[i - 1].BaseAltitudeM), $"band {i}");
        }

        [Test]
        public void Bands_AreContinuous()
        {
            // Extrapolating a band to the start of the next one must give that band's base density.
            // Catches typos when copying the table.
            for (int i = 0; i < Atmosphere.Bands.Length - 1; i++)
            {
                var b = Atmosphere.Bands[i];
                var next = Atmosphere.Bands[i + 1];
                double fromBelow = b.BaseDensity * System.Math.Exp(-(next.BaseAltitudeM - b.BaseAltitudeM) / b.ScaleHeightM);
                Assert.That(fromBelow, Is.EqualTo(next.BaseDensity).Within(0.5).Percent, $"between {b.BaseAltitudeM / 1e3} km and {next.BaseAltitudeM / 1e3} km");
            }
        }

        [Test]
        public void Density_StrictlyFallsWithAltitude()
        {
            double previous = double.MaxValue;
            for (int km = 0; km <= 1000; km++)
            {
                double rho = Atmosphere.Density(km * 1e3);
                Assert.That(rho, Is.LessThan(previous), $"at {km} km");
                previous = rho;
            }
        }

        [Test]
        public void ActivityFactor_ScalesThermosphere()
        {
            Assert.That(Atmosphere.Density(400e3, 3.0), Is.EqualTo(3 * Atmosphere.Density(400e3)).Within(1e-9).Percent);
            Assert.That(Atmosphere.Density(100e3, 0.5), Is.EqualTo(0.5 * Atmosphere.Density(100e3)).Within(1e-9).Percent);   // 100 km counts
        }

        [Test]
        public void ActivityFactor_IgnoredBelow100km()
        {
            Assert.That(Atmosphere.Density(50e3, 3.0), Is.EqualTo(Atmosphere.Density(50e3)));
            Assert.That(Atmosphere.Density(99.9e3, 3.0), Is.EqualTo(Atmosphere.Density(99.9e3)));
        }

        [Test]
        public void Density_BelowGround_IsSeaLevel()
        {
            Assert.That(Atmosphere.Density(-10), Is.EqualTo(1.225));
            Assert.That(Atmosphere.Density(-5000), Is.EqualTo(1.225));
        }

        [Test]
        public void Density_AboveTable_StillFalls()
        {
            double rho = Atmosphere.Density(1200e3);
            Assert.That(rho, Is.GreaterThan(0));
            Assert.That(rho, Is.LessThan(Atmosphere.Density(1000e3)));
        }

        [Test]
        public void RealSimRules_HaveAllActivityFactors()
        {
            string json = File.ReadAllText(TestHelpers.RulesFile("sim_rules.json"));
            var sim = CatalogLoader.LoadObject<SimRules>(json, "sim_rules.json");
            Assert.That(sim.DensityFactor(SolarActivity.Low), Is.EqualTo(0.5));
            Assert.That(sim.DensityFactor(SolarActivity.Medium), Is.EqualTo(1.0));
            Assert.That(sim.DensityFactor(SolarActivity.High), Is.EqualTo(3.0));
        }
    }
}
