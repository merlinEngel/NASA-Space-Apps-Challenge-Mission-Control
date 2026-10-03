using System.Collections.Generic;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class CostBudgetTests
    {
        const double Tol = 1e-6;

        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec> { new PropulsionSpec { id = "cold_gas", ispS = 70, dryMassKg = 0.8, priceUsd = 60000 } },
            new List<InstrumentSpec>
            {
                new InstrumentSpec { id = "camera", massKg = 3, priceUSD = 1500000 },
                new InstrumentSpec { id = "mag", massKg = 1, priceUSD = 300000 },
            },
            new List<LauncherSpec>
            {
                new LauncherSpec { id = "electron", leoKg = 300, ssoKg = 200, priceUSD = 7500000, successRate = 0.9 },
                new LauncherSpec
                {
                    id = "transporter", type = LaunchType.Rideshare, successRate = 0.99,
                    pricePerKgUSD = 6500, minBillableMassKg = 50, maxPayloadPerCustomerKg = 500,
                    orbits = new[] { LaunchOrbit.Sso }
                },
            },
            new List<CommsSpec> { new CommsSpec { id = "s_band", massKg = 0.3, priceUSD = 50000 } },
            new List<BatterySpec> { new BatterySpec { id = "liion", massKg = 0.65, priceUSD = 15000 } },
            new List<PlatformSpec>
            {
                new PlatformSpec { id = "cubesat_6u", busMassKg = 5, maxMassKg = 12, priceUSD = 800000 },
                new PlatformSpec { id = "smallsat", busMassKg = 80, maxMassKg = 150, priceUSD = 8000000 },
            },
            new List<GroundStationSpec>(),
            new List<SolarCellSpec> { new SolarCellSpec { id = "gaas", massPerAreaKgM2 = 3.5, pricePerAreaUSDM2 = 400000 } },
            new List<MissionTemplate> { new MissionTemplate { id = "techdemo", budgetUSD = 5000000 } },
            new BalanceRules { MassMarginEarlyPhase = 0.25 }, null, null, null);

        static MissionDesign SixU() => new MissionDesign
        {
            MissionId = "techdemo",
            PlatformId = "cubesat_6u",
            InstrumentIds = new List<string> { "camera", "mag" },
            LauncherId = "transporter",
            PropulsionId = "cold_gas",
            CommsId = "s_band",
            BatteryId = "liion",
            BatteryCount = 2,
            SolarCellId = "gaas",
            SolarAreaM2 = 0.2,
            AltitudeM = 500000,
            InclinationDeg = 97.5
        };

        // hardware = platform 800,000 + instruments (1,500,000 + 300,000) + propulsion 60,000
        //          + comms 50,000 + battery 15,000 x 2 + solar 0.2 m2 x 400,000
        //          = 800,000 + 1,800,000 + 60,000 + 50,000 + 30,000 + 80,000 = 2,820,000 USD
        [Test]
        public void HardwareCost_MatchesHandCalculation()
        {
            Assert.That(CostBudget.GetHardwareCost(TestCatalog(), SixU()), Is.EqualTo(2820000).Within(Tol));
        }

        // dry = 5 + 3 + 1 + 0.8 + 0.3 + 0.65 x 2 + 0.2 x 3.5 = 12.1 kg, total = 12.1 x 1.25 = 15.125 kg
        // rideshare bills max(15.125, 50) = 50 kg x 6,500 = 325,000 USD
        // total = 2,820,000 + 325,000 = 3,145,000 USD, reserve = 5,000,000 - 3,145,000 = 1,855,000 USD
        [Test]
        public void SixU_OnRideshare_MatchesHandCalculation()
        {
            var r = CostBudget.Evaluate(TestCatalog(), SixU());
            Assert.That(r.HardwareCostUSD, Is.EqualTo(2820000).Within(Tol));
            Assert.That(r.LaunchCostUSD, Is.EqualTo(325000).Within(Tol));
            Assert.That(r.OperationsCostUSD, Is.EqualTo(0));
            Assert.That(r.BudgetUSD, Is.EqualTo(5000000));
            Assert.That(r.TotalCostUSD, Is.EqualTo(3145000).Within(Tol));
            Assert.That(r.ReserveUSD, Is.EqualTo(1855000).Within(Tol));
            Assert.That(r.ReserveFraction, Is.EqualTo(0.371).Within(Tol));
        }

        [Test]
        public void BatteryCount_MultipliesBatteryPrice()
        {
            var d = SixU();
            d.BatteryCount = 4;   // 2 more batteries = +30,000 USD
            Assert.That(CostBudget.GetHardwareCost(TestCatalog(), d), Is.EqualTo(2850000).Within(Tol));
        }

        [Test]
        public void DedicatedLaunch_IsFixedPrice()
        {
            var d = SixU();
            d.LauncherId = "electron";
            Assert.That(CostBudget.Evaluate(TestCatalog(), d).LaunchCostUSD, Is.EqualTo(7500000));
        }

        // smallsat: dry = 80 + 3 + 1 + 0.8 + 0.3 + 1.3 + 0.7 = 87.1 kg, total = 108.875 kg
        // rideshare: 108.875 kg x 6,500 = 707,687.5 USD (above the 50 kg minimum)
        [Test]
        public void Rideshare_AboveMinimum_BillsTotalMassWithMargin()
        {
            var d = SixU();
            d.PlatformId = "smallsat";
            Assert.That(CostBudget.Evaluate(TestCatalog(), d).LaunchCostUSD, Is.EqualTo(707687.5).Within(Tol));
        }

        [Test]
        public void NoLauncher_HasNoLaunchCost()
        {
            var d = SixU();
            d.LauncherId = null;
            Assert.That(CostBudget.Evaluate(TestCatalog(), d).LaunchCostUSD, Is.EqualTo(0));
        }

        [Test]
        public void NoMission_GivesBudgetZeroAndReserveFractionZero()
        {
            var d = SixU();
            d.MissionId = null;
            var r = CostBudget.Evaluate(TestCatalog(), d);
            Assert.That(r.BudgetUSD, Is.EqualTo(0));
            Assert.That(r.ReserveUSD, Is.EqualTo(-3145000).Within(Tol));
            Assert.That(r.ReserveFraction, Is.EqualTo(0));
        }

        [Test]
        public void EmptyDesign_DoesNotCrashAndCostsNothing()
        {
            CostBudgetResult r = null;
            Assert.DoesNotThrow(() => r = CostBudget.Evaluate(TestCatalog(), new MissionDesign()));
            Assert.That(r.TotalCostUSD, Is.EqualTo(0));
            Assert.That(r.ReserveFraction, Is.EqualTo(0));
        }
    }
}
