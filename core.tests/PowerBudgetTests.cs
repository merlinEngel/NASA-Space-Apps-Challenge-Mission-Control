using System;
using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class PowerBudgetTests
    {
        const double Tol = 1e-6;

        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec>(),
            new List<InstrumentSpec> { new InstrumentSpec { id = "camera", massKg = 3, powerW = 6 } },
            new List<LauncherSpec>(),
            new List<CommsSpec> { new CommsSpec { id = "uhf", massKg = 0.1, powerW = 2 } },
            new List<BatterySpec> { new BatterySpec { id = "liion", energyWh = 20, massKg = 0.2, maxDepthOfDischarge = 0.3 } },
            new List<PlatformSpec> { new PlatformSpec { id = "cubesat_3u", busMassKg = 2.5, maxMassKg = 6, busPowerW = 3 } },
            new List<GroundStationSpec>(),
            new List<SolarCellSpec>
            {
                new SolarCellSpec { id = "gaas", efficiency = 0.30, packingFactor = 0.85, degradationPerYear = 0.01 },
            },
            new List<MissionTemplate> { new MissionTemplate { id = "techdemo", durationDays = 90 } },
            new BalanceRules { PowerMarginEarlyPhase = 0.25 }, null, null, null);

        static MissionDesign Design(int batteryCount = 2, double solarAreaM2 = 0.06) => new MissionDesign
        {
            MissionId = "techdemo",
            PlatformId = "cubesat_3u",
            InstrumentIds = new List<string> { "camera" },
            CommsId = "uhf",
            BatteryId = "liion",
            BatteryCount = batteryCount,
            SolarCellId = "gaas",
            SolarAreaM2 = solarAreaM2,
            AltitudeM = 500000
        };

        // a = 6,378,137 + 500,000 = 6,878,137 m, T = 2 pi sqrt(a^3 / mu) = 5,677 s (94.6 min)
        [Test]
        public void OrbitPeriod_At500Km_IsAbout5677s()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), Design()).OrbitPeriodS, Is.EqualTo(5677).Within(1));
        }

        // asin(6,378,137 / 6,878,137) / pi = 1.18669 / pi = 0.3777
        [Test]
        public void EclipseFraction_At500Km_IsAbout0378()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), Design()).EclipseFraction, Is.EqualTo(0.378).Within(0.001));
        }

        // consumption = bus 3 + camera 6 + uhf 2 = 11 W, margin = 11 x 0.25 = 2.75 W, required = 13.75 W
        [Test]
        public void Consumption_MarginAndRequired()
        {
            var r = PowerBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.ConsumptionW, Is.EqualTo(11).Within(Tol));
            Assert.That(r.MarginW, Is.EqualTo(2.75).Within(Tol));
            Assert.That(r.RequiredW, Is.EqualTo(13.75).Within(Tol));
        }

        // Without degradation: 0.06 m2 x 1361 W/m2 x 0.30 x 0.85 = 20.82 W
        [Test]
        public void SunGeneration_IsAreaTimesSolarConstantTimesEfficiencyTimesPacking()
        {
            var r = PowerBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.SunGenerationW, Is.EqualTo(0.06 * 1361 * 0.30 * 0.85).Within(0.1));
        }

        // End of life after 90 days: 20.82 W x 0.99^(90 / 365.25) = 20.77 W
        [Test]
        public void SunGeneration_IncludesDegradationOverMission()
        {
            var r = PowerBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.SunGenerationW, Is.EqualTo(0.06 * 1361 * 0.30 * 0.85 * Math.Pow(0.99, 90 / Constants.DaysPerYear)).Within(Tol));
        }

        // average = sun x (1 - eclipse); reserve = average - required
        [Test]
        public void AverageGenerationAndReserve()
        {
            var r = PowerBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.AverageGenerationW, Is.EqualTo(r.SunGenerationW * (1 - r.EclipseFraction)).Within(Tol));
            Assert.That(r.ReserveW, Is.EqualTo(r.AverageGenerationW - 13.75).Within(Tol));
        }

        // need = 13.75 W x 0.3777 x 5,677 s / 3,600 = 8.19 Wh
        [Test]
        public void EclipseEnergyNeed_IsRequiredPowerOverEclipse()
        {
            var r = PowerBudget.Evaluate(TestCatalog(), Design());
            Assert.That(r.EclipseEnergyNeedWh, Is.EqualTo(13.75 * r.EclipseFraction * r.OrbitPeriodS / 3600).Within(Tol));
            Assert.That(r.EclipseEnergyNeedWh, Is.EqualTo(8.19).Within(0.01));
        }

        // 2 x 20 Wh x 0.3 = 12 Wh usable >= 8.19 Wh need
        [Test]
        public void TwoBatteries_CoverEclipse()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), Design(batteryCount: 2)).BatteryOk, Is.True);
        }

        // usable = energy x max depth of discharge x count = 20 x 0.3 x 2 = 12 Wh
        [Test]
        public void BatteryUsable_IsEnergyTimesDepthOfDischargeTimesCount()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), Design(batteryCount: 2)).BatteryUsableWh, Is.EqualTo(12).Within(Tol));
        }

        // 1 x 20 Wh x 0.3 = 6 Wh usable < 8.19 Wh need
        [Test]
        public void OneBattery_DoesNotCoverEclipse()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), Design(batteryCount: 1)).BatteryOk, Is.False);
        }

        [Test]
        public void EmptyDesign_DoesNotCrash()
        {
            PowerBudgetResult r = null;
            Assert.DoesNotThrow(() => r = PowerBudget.Evaluate(TestCatalog(), new MissionDesign()));
            Assert.That(r.ConsumptionW, Is.EqualTo(0));
            Assert.That(r.SunGenerationW, Is.EqualTo(0));
            Assert.That(double.IsNaN(r.EclipseEnergyNeedWh), Is.False);
        }

        [Test]
        public void EmptyDesign_ReserveFractionIsNotNaN()
        {
            Assert.That(PowerBudget.Evaluate(TestCatalog(), new MissionDesign()).ReserveFraction, Is.EqualTo(0));
        }
    }
}
