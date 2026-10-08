using System.Collections.Generic;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class MassBudgetTests
    {
        const double Tol = 1e-9;

        // Hand-built catalog so the expected numbers do not change when the JSON files do.
        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec> { new PropulsionSpec { id = "cold_gas", ispS = 70, thrustN = 1, dryMassKg = 0.8 } },
            new List<InstrumentSpec>
            {
                new InstrumentSpec { id = "mag", massKg = 0.3 },
                new InstrumentSpec { id = "camera", massKg = 0.5 },
            },
            new List<LauncherSpec>
            {
                new LauncherSpec { id = "electron", leoKg = 300, ssoKg = 200, priceUSD = 7500000, successRate = 0.9 },
                new LauncherSpec { id = "no_sso", leoKg = 500, priceUSD = 1, successRate = 0.9 },
                new LauncherSpec
                {
                    id = "transporter", type = LaunchType.Rideshare, successRate = 0.99,
                    pricePerKgUSD = 6500, minBillableMassKg = 50, maxPayloadPerCustomerKg = 150,
                    orbits = new[] { LaunchOrbit.Sso }
                },
            },
            new List<CommsSpec> { new CommsSpec { id = "uhf", massKg = 0.1 } },
            new List<BatterySpec> { new BatterySpec { id = "liion", massKg = 0.2 } },
            new List<PlatformSpec>
            {
                new PlatformSpec { id = "cubesat_3u", busMassKg = 2.0, maxMassKg = 6.0 },
                new PlatformSpec { id = "smallsat", busMassKg = 50, maxMassKg = 1000 },
            },
            new List<GroundStationSpec>(),
            new List<SolarCellSpec> { new SolarCellSpec { id = "gaas", massPerAreaKgM2 = 2.0 } },
            new List<MissionTemplate>(),
            new BalanceRules { MassMarginEarlyPhase = 0.25 }, null, null, null, new List<OrbitPresetSpec>());

        static MissionDesign ThreeU() => new MissionDesign
        {
            PlatformId = "cubesat_3u",
            InstrumentIds = new List<string> { "mag", "camera" },
            LauncherId = "electron",
            PropulsionId = "cold_gas",
            CommsId = "uhf",
            BatteryId = "liion",
            BatteryCount = 2,
            SolarCellId = "gaas",
            SolarAreaM2 = 0.06,
            PropellantMassKg = 0.1,
            AltitudeM = 500000,
            InclinationDeg = 51.6
        };

        // Large platform, so the launcher is the smaller limit.
        static MissionDesign SmallSat(string launcherId, double inclinationDeg, double altitudeM = 500000) => new MissionDesign
        {
            PlatformId = "smallsat", LauncherId = launcherId, AltitudeM = altitudeM, InclinationDeg = inclinationDeg
        };

        // ---------- Hand calculation ----------

        // dry   = bus 2.0 + instruments (0.3 + 0.5) + comms 0.1 + battery 0.2 x 2
        //       + solar 0.06 m2 x 2.0 kg/m2 + propulsion 0.8
        //       = 2.0 + 0.8 + 0.1 + 0.4 + 0.12 + 0.8 = 4.22 kg
        // margin = 4.22 x 0.25 = 1.055 kg
        // total  = 4.22 + 1.055 + propellant 0.1 = 5.375 kg
        // limit  = min(electron LEO 300, platform max 6.0) = 6.0 kg
        // reserve = 6.0 - 5.375 = 0.625 kg = 0.625 / 6.0 = 0.1041666...
        [Test]
        public void ThreeU_MatchesHandCalculation()
        {
            var r = MassBudget.Evaluate(TestCatalog(), ThreeU());

            Assert.That(r.DryMassKg, Is.EqualTo(4.22).Within(Tol));
            Assert.That(r.MarginKg, Is.EqualTo(1.055).Within(Tol));
            Assert.That(r.PropellantMassKg, Is.EqualTo(0.1).Within(Tol));
            Assert.That(r.TotalMassKg, Is.EqualTo(5.375).Within(Tol));
            Assert.That(r.LimitKg, Is.EqualTo(6.0).Within(Tol));
            Assert.That(r.ReserveKg, Is.EqualTo(0.625).Within(Tol));
            Assert.That(r.ReserveFraction, Is.EqualTo(0.625 / 6.0).Within(Tol));
        }

        [Test]
        public void GetDryMass_MatchesEvaluate()
        {
            Assert.That(MassBudget.GetDryMass(TestCatalog(), ThreeU()), Is.EqualTo(4.22).Within(Tol));
        }

        [Test]
        public void BatteryCount_MultipliesBatteryMass()
        {
            var d = ThreeU();
            d.BatteryCount = 5;   // 3 more batteries = +0.6 kg
            Assert.That(MassBudget.GetDryMass(TestCatalog(), d), Is.EqualTo(4.82).Within(Tol));
        }

        // ---------- Limits ----------

        [Test]
        public void DedicatedLeo_UsesLeoKg()
        {
            var r = MassBudget.Evaluate(TestCatalog(), SmallSat("electron", 51.6));
            Assert.That(r.LimitKg, Is.EqualTo(300));
        }

        [Test]
        public void SsoInclination_UsesSsoKg()
        {
            var r = MassBudget.Evaluate(TestCatalog(), SmallSat("electron", 97.5));
            Assert.That(r.LimitKg, Is.EqualTo(200));
        }

        [Test]
        public void SsoInclination_WithoutSsoValue_FallsBackToLeoKg()
        {
            var r = MassBudget.Evaluate(TestCatalog(), SmallSat("no_sso", 97.5));
            Assert.That(r.LimitKg, Is.EqualTo(500));
        }

        [Test]
        public void Rideshare_UsesMaxPayloadPerCustomer()
        {
            var r = MassBudget.Evaluate(TestCatalog(), SmallSat("transporter", 97.5));
            Assert.That(r.LimitKg, Is.EqualTo(150));
        }

        [Test]
        public void Limit_IsSmallerOfLauncherAndPlatform()
        {
            // 3U platform max 6 kg < electron LEO 300 kg
            Assert.That(MassBudget.Evaluate(TestCatalog(), ThreeU()).LimitKg, Is.EqualTo(6.0));
            // smallsat max 1000 kg > electron LEO 300 kg
            Assert.That(MassBudget.Evaluate(TestCatalog(), SmallSat("electron", 51.6)).LimitKg, Is.EqualTo(300));
        }

        // Current behaviour: no launcher capacity is known above 2000 km, so only the platform limits.
        [Test]
        public void AltitudeAbove2000Km_FallsBackToPlatformLimit()
        {
            var r = MassBudget.Evaluate(TestCatalog(), SmallSat("electron", 51.6, altitudeM: 2000001));
            Assert.That(r.LimitKg, Is.EqualTo(1000));
        }

        // ---------- Missing and unknown parts ----------

        [Test]
        public void NoPropulsion_DoesNotCrashAndCountsNoEngine()
        {
            var d = ThreeU();
            d.PropulsionId = null;
            d.PropellantMassKg = 0;
            MassBudgetResult r = null;
            Assert.DoesNotThrow(() => r = MassBudget.Evaluate(TestCatalog(), d));
            Assert.That(r.DryMassKg, Is.EqualTo(4.22 - 0.8).Within(Tol));
        }

        [Test]
        public void NullPartIds_ExceptLauncher_DoNotCrash()
        {
            var d = new MissionDesign { LauncherId = "electron", AltitudeM = 500000 };
            MassBudgetResult r = null;
            Assert.DoesNotThrow(() => r = MassBudget.Evaluate(TestCatalog(), d));
            Assert.That(r.DryMassKg, Is.EqualTo(0));
            Assert.That(r.LimitKg, Is.EqualTo(300));   // no platform, so only the launcher limits
        }

        // Current behaviour: unknown ids are skipped silently, a validator has to report them.
        [Test]
        public void UnknownInstrumentId_IsIgnored()
        {
            var d = ThreeU();
            d.InstrumentIds.Add("warp_scanner");
            Assert.That(MassBudget.GetDryMass(TestCatalog(), d), Is.EqualTo(4.22).Within(Tol));
        }

        [Test]
        public void UnknownLauncherId_FallsBackToPlatformLimit()
        {
            var d = ThreeU();
            d.LauncherId = "saturn_v";
            Assert.That(MassBudget.Evaluate(TestCatalog(), d).LimitKg, Is.EqualTo(6.0));
        }

        [Test]
        public void NullLauncherId_DoesNotCrashAndUsesPlatformLimit()
        {
            var d = ThreeU();
            d.LauncherId = null;
            MassBudgetResult r = null;
            Assert.DoesNotThrow(() => r = MassBudget.Evaluate(TestCatalog(), d));
            Assert.That(r.LimitKg, Is.EqualTo(6.0));
        }

        // Current behaviour: with neither platform nor launcher there is no limit at all.
        [Test]
        public void EmptyDesign_HasInfiniteLimit()
        {
            var r = MassBudget.Evaluate(TestCatalog(), new MissionDesign());
            Assert.That(r.LimitKg, Is.EqualTo(double.PositiveInfinity));
            Assert.That(r.TotalMassKg, Is.EqualTo(0));
        }
    }
}
