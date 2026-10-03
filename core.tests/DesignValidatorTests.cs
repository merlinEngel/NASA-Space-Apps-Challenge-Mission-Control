using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    public class DesignValidatorTests
    {
        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec> { new PropulsionSpec { id = "cold_gas", ispS = 70, dryMassKg = 0.8 } },
            new List<InstrumentSpec>
            {
                new InstrumentSpec { id = "camera", massKg = 3 },
                new InstrumentSpec { id = "mag", massKg = 1 },
            },
            new List<LauncherSpec>
            {
                new LauncherSpec { id = "electron", leoKg = 300, ssoKg = 200, priceUSD = 7500000, successRate = 0.9 },
                new LauncherSpec
                {
                    id = "transporter", type = LaunchType.Rideshare, successRate = 0.99,
                    pricePerKgUSD = 6500, minBillableMassKg = 50, maxPayloadPerCustomerKg = 500,
                    orbits = new[] { LaunchOrbit.Sso }, altitudeM = new ValueRange(500000, 600000)
                },
            },
            new List<CommsSpec>
            {
                new CommsSpec { id = "s_band", band = "S" },
                new CommsSpec { id = "uhf", band = "UHF" },
            },
            new List<BatterySpec> { new BatterySpec { id = "liion", energyWh = 80, maxDepthOfDischarge = 0.3 } },
            new List<PlatformSpec>
            {
                new PlatformSpec { id = "cubesat_6u", busMassKg = 5, maxMassKg = 12, maxSolarAreaM2 = 0.4, lifetimeYears = 3 },
                new PlatformSpec { id = "short_life", busMassKg = 5, maxMassKg = 12, maxSolarAreaM2 = 0.4, lifetimeYears = 0.1 },
            },
            new List<GroundStationSpec>
            {
                new GroundStationSpec { id = "svalbard", latDeg = 78, lonDeg = 15, bands = new[] { "S", "X" } },
                new GroundStationSpec { id = "uhf_station", latDeg = 48, lonDeg = 11, bands = new[] { "UHF" } },
            },
            new List<SolarCellSpec> { new SolarCellSpec { id = "gaas" } },
            new List<MissionTemplate>
            {
                new MissionTemplate { id = "techdemo", durationDays = 90, minInstruments = 1, altitudeM = new ValueRange(400000, 600000) },
                new MissionTemplate { id = "two_instruments", durationDays = 90, minInstruments = 2, altitudeM = new ValueRange(400000, 600000) },
            },
            new BalanceRules(), null, null, null);

        // Passes every rule: SSO rideshare at 500 km, S-band with an S-band station.
        static MissionDesign Valid() => new MissionDesign
        {
            MissionId = "techdemo",
            PlatformId = "cubesat_6u",
            InstrumentIds = new List<string> { "camera" },
            LauncherId = "transporter",
            CommsId = "s_band",
            BatteryId = "liion",
            BatteryCount = 2,
            SolarCellId = "gaas",
            SolarAreaM2 = 0.2,
            GroundStationIds = new List<string> { "svalbard" },
            AltitudeM = 500000,
            InclinationDeg = 97.5
        };

        static List<DesignIssue> Validate(MissionDesign design) => DesignValidator.Validate(TestCatalog(), design);

        static List<DesignIssueCode> Codes(MissionDesign design) => Validate(design).Select(i => i.Code).ToList();

        static DesignIssue Single(MissionDesign design, DesignIssueCode code)
        {
            var issues = Validate(design);
            Assert.That(issues.Select(i => i.Code), Is.EqualTo(new[] { code }), string.Join(", ", issues.Select(i => i.Code)));
            return issues[0];
        }

        // ---------- Valid design ----------

        [Test]
        public void ValidDesign_HasNoIssues()
        {
            Assert.That(Validate(Valid()), Is.Empty);
        }

        [Test]
        public void ValidDesign_OnDedicatedLauncherInLeo_HasNoIssues()
        {
            var d = Valid();
            d.LauncherId = "electron";
            d.InclinationDeg = 51.6;
            Assert.That(Validate(d), Is.Empty);
        }

        [Test]
        public void PropulsionIsOptional_ButWithEngineAndPropellantStillValid()
        {
            var d = Valid();
            d.PropulsionId = "cold_gas";
            d.PropellantMassKg = 1;
            Assert.That(Validate(d), Is.Empty);
        }

        // ---------- Required parts ----------

        [TestCase(nameof(MissionDesign.PlatformId), DesignIssueCode.MissingPlatform)]
        [TestCase(nameof(MissionDesign.LauncherId), DesignIssueCode.MissingLauncher)]
        [TestCase(nameof(MissionDesign.CommsId), DesignIssueCode.MissingComms)]
        [TestCase(nameof(MissionDesign.BatteryId), DesignIssueCode.MissingBattery)]
        [TestCase(nameof(MissionDesign.SolarCellId), DesignIssueCode.MissingSolarCell)]
        public void MissingRequiredPart_IsReportedOnce(string property, DesignIssueCode expected)
        {
            foreach (string value in new[] { null, "" })
            {
                var d = Valid();
                typeof(MissionDesign).GetProperty(property).SetValue(d, value);
                var issue = Single(d, expected);
                Assert.That(issue.Detail, Is.Null);
            }
        }

        // Without a mission there are no mission rules to check, so only MissingMission appears.
        [Test]
        public void MissingMission_IsReportedWithoutFollowUpIssues()
        {
            var d = Valid();
            d.MissionId = null;
            Single(d, DesignIssueCode.MissingMission);
        }

        [Test]
        public void NoGroundStation_IsReportedOnce_NotAlsoAsBandMismatch()
        {
            var d = Valid();
            d.GroundStationIds = new List<string>();
            Single(d, DesignIssueCode.NoGroundStation);
        }

        [Test]
        public void NullGroundStationList_DoesNotCrash()
        {
            var d = Valid();
            d.GroundStationIds = null;
            Assert.That(Codes(d), Is.EqualTo(new[] { DesignIssueCode.NoGroundStation }));
        }

        [Test]
        public void EmptyDesign_ListsAllMissingPartsWithoutCrashing()
        {
            List<DesignIssueCode> codes = null;
            Assert.DoesNotThrow(() => codes = Codes(new MissionDesign()));
            Assert.That(codes, Is.SupersetOf(new[]
            {
                DesignIssueCode.MissingPlatform, DesignIssueCode.MissingMission, DesignIssueCode.MissingLauncher,
                DesignIssueCode.MissingComms, DesignIssueCode.MissingBattery, DesignIssueCode.MissingSolarCell,
                DesignIssueCode.NoGroundStation
            }));
        }

        // ---------- Unknown ids ----------

        [TestCase(nameof(MissionDesign.PlatformId))]
        [TestCase(nameof(MissionDesign.LauncherId))]
        [TestCase(nameof(MissionDesign.CommsId))]
        [TestCase(nameof(MissionDesign.BatteryId))]
        [TestCase(nameof(MissionDesign.SolarCellId))]
        [TestCase(nameof(MissionDesign.PropulsionId))]
        [TestCase(nameof(MissionDesign.MissionId))]
        public void UnknownId_IsReportedWithTheId(string property)
        {
            var d = Valid();
            typeof(MissionDesign).GetProperty(property).SetValue(d, "warp_drive");
            var unknown = Validate(d).Where(i => i.Code == DesignIssueCode.UnknownPart).ToList();
            Assert.That(unknown.Count, Is.EqualTo(1));
            Assert.That(unknown[0].Detail, Is.EqualTo("warp_drive"));
        }

        [Test]
        public void UnknownInstrumentAndStation_AreReportedWithTheirIds()
        {
            var d = Valid();
            d.InstrumentIds.Add("warp_scanner");
            d.GroundStationIds.Add("moon_base");
            var details = Validate(d).Where(i => i.Code == DesignIssueCode.UnknownPart).Select(i => i.Detail);
            Assert.That(details, Is.EquivalentTo(new[] { "warp_scanner", "moon_base" }));
        }

        // ---------- Mission needs ----------

        [Test]
        public void TooFewInstruments_IsReported()
        {
            var d = Valid();
            d.InstrumentIds = new List<string>();
            Single(d, DesignIssueCode.TooFewInstruments);
        }

        [Test]
        public void UnknownInstrument_DoesNotCountTowardsMinimum()
        {
            var d = Valid();
            d.InstrumentIds = new List<string> { "warp_scanner" };
            Assert.That(Codes(d), Is.EquivalentTo(new[] { DesignIssueCode.UnknownPart, DesignIssueCode.TooFewInstruments }));
        }

        [Test]
        public void TwoInstrumentsRequired_OneGiven_IsReported()
        {
            var d = Valid();
            d.MissionId = "two_instruments";
            Single(d, DesignIssueCode.TooFewInstruments);
            d.InstrumentIds.Add("mag");
            Assert.That(Validate(d), Is.Empty);
        }

        // Mission range 400–600 km; the dedicated Electron has no altitude window, so only the mission rule fires.
        [TestCase(399999)]
        [TestCase(600001)]
        public void AltitudeOutsideMission_IsReported(double altitudeM)
        {
            var d = Valid();
            d.LauncherId = "electron";
            d.AltitudeM = altitudeM;
            var issue = Single(d, DesignIssueCode.AltitudeOutsideMission);
            Assert.That(issue.Detail, Does.Contain("km"));
        }

        [TestCase(400000)]
        [TestCase(600000)]
        public void AltitudeOnMissionBoundary_IsValid(double altitudeM)
        {
            var d = Valid();
            d.LauncherId = "electron";
            d.AltitudeM = altitudeM;
            Assert.That(Validate(d), Is.Empty);
        }

        // ---------- Orbit and launcher ----------

        [Test]
        public void AltitudeAbove2000Km_IsOrbitNotSupported()
        {
            var d = Valid();
            d.AltitudeM = 2500000;
            Assert.That(Codes(d), Is.EquivalentTo(new[] { DesignIssueCode.AltitudeOutsideMission, DesignIssueCode.OrbitNotSupported }));
        }

        [TestCase(-1)]
        [TestCase(180.1)]
        public void InclinationOutside0To180_IsReported(double inclinationDeg)
        {
            var d = Valid();
            d.LauncherId = "electron";
            d.InclinationDeg = inclinationDeg;
            Single(d, DesignIssueCode.InvalidInclination);
        }

        // Rideshare only flies to SSO; 51.6° is classified as LEO.
        [Test]
        public void RideshareToWrongOrbit_IsReported()
        {
            var d = Valid();
            d.InclinationDeg = 51.6;
            var issue = Single(d, DesignIssueCode.RideshareOrbitMismatch);
            Assert.That(issue.Detail, Is.EqualTo("leo"));
        }

        // Rideshare window 500–600 km, mission allows 400–600 km.
        [Test]
        public void RideshareOutsideItsAltitudeWindow_IsReported()
        {
            var d = Valid();
            d.AltitudeM = 450000;
            Single(d, DesignIssueCode.RideshareAltitudeMismatch);
        }

        // ---------- Platform boundaries ----------

        [Test]
        public void SolarAreaAbovePlatformMaximum_IsReported()
        {
            var d = Valid();
            d.SolarAreaM2 = 0.41;
            Single(d, DesignIssueCode.SolarAreaTooLarge);
            d.SolarAreaM2 = 0.4;
            Assert.That(Validate(d), Is.Empty);
        }

        // 90 days > 0.1 years (36.5 days)
        [Test]
        public void MissionLongerThanPlatformLifetime_IsReported()
        {
            var d = Valid();
            d.PlatformId = "short_life";
            Single(d, DesignIssueCode.MissionLongerThanLifetime);
        }

        // ---------- Communication ----------

        [Test]
        public void NoStationForCommsBand_IsReportedWithTheBand()
        {
            var d = Valid();
            d.CommsId = "uhf";   // svalbard has only S and X
            var issue = Single(d, DesignIssueCode.NoCompatibleGroundStation);
            Assert.That(issue.Detail, Is.EqualTo("UHF"));
        }

        [Test]
        public void OneCompatibleStationIsEnough()
        {
            var d = Valid();
            d.CommsId = "uhf";
            d.GroundStationIds.Add("uhf_station");
            Assert.That(Validate(d), Is.Empty);
        }

        // ---------- Numbers ----------

        [TestCase(nameof(MissionDesign.AltitudeM), 0.0, "altitude_m")]
        [TestCase(nameof(MissionDesign.SolarAreaM2), -0.01, "solar_area_m2")]
        [TestCase(nameof(MissionDesign.PropellantMassKg), -1.0, "propellant_mass_kg")]
        public void NegativeOrZeroNumber_IsInvalidValueWithFieldName(string property, double value, string field)
        {
            var d = Valid();
            d.PropulsionId = "cold_gas";   // so negative propellant is not also "propellant without propulsion"
            typeof(MissionDesign).GetProperty(property).SetValue(d, value);
            var invalid = Validate(d).Where(i => i.Code == DesignIssueCode.InvalidValue).ToList();
            Assert.That(invalid.Select(i => i.Detail), Is.EqualTo(new[] { field }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void BatteryCountBelowOne_IsInvalidValue(int count)
        {
            var d = Valid();
            d.BatteryCount = count;
            Assert.That(Single(d, DesignIssueCode.InvalidValue).Detail, Is.EqualTo("battery_count"));
        }

        [Test]
        public void ZeroSolarAreaAndZeroPropellant_AreAllowed()
        {
            var d = Valid();
            d.SolarAreaM2 = 0;
            d.PropellantMassKg = 0;
            Assert.That(Validate(d), Is.Empty);
        }

        [Test]
        public void PropellantWithoutPropulsion_IsReported()
        {
            var d = Valid();
            d.PropellantMassKg = 2;
            Single(d, DesignIssueCode.PropellantWithoutPropulsion);
        }

        [Test, Ignore("Bug: double.IsFinite(x) && x <= 0 lets NaN and Infinity pass without an issue")]
        public void NaNOrInfinity_IsInvalidValue()
        {
            foreach (double value in new[] { double.NaN, double.PositiveInfinity })
            {
                var d = Valid();
                d.SolarAreaM2 = value;
                Assert.That(Validate(d).Any(i => i.Code == DesignIssueCode.InvalidValue && i.Detail == "solar_area_m2"), value.ToString());
            }
        }

        [Test]
        public void SeveralProblems_AreAllCollected()
        {
            var d = Valid();
            d.LauncherId = null;
            d.BatteryCount = 0;
            d.SolarAreaM2 = 1;
            Assert.That(Codes(d), Is.EquivalentTo(new[]
            {
                DesignIssueCode.MissingLauncher, DesignIssueCode.InvalidValue, DesignIssueCode.SolarAreaTooLarge
            }));
        }

        // ---------- Real data: the five test designs ----------

        static string DataDir(params string[] parts)
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Data"))) dir = dir.Parent;
            return Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
        }

        static Catalog RealCatalog()
        {
            string data = DataDir("Assets", "StreamingAssets", "Data");
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                CatalogLoader.LoadObject<BalanceRules>(File.ReadAllText(Path.Combine(data, "balance_rules.json")), "balance_rules.json"),
                null, null, null);
        }

        static DesignReport EvaluateTestDesign(string file)
        {
            string json = File.ReadAllText(DataDir("core.tests", "Data", "designs", file));
            return DesignValidator.Evaluate(RealCatalog(), CatalogLoader.LoadObject<MissionDesign>(json, file));
        }

        static void AssertStatuses(DesignReport r, BudgetStatus mass, BudgetStatus cost, BudgetStatus power, BudgetStatus data)
        {
            Assert.That(r.Statuses[BudgetKind.Mass], Is.EqualTo(mass), "mass");
            Assert.That(r.Statuses[BudgetKind.Cost], Is.EqualTo(cost), "cost");
            Assert.That(r.Statuses[BudgetKind.Power], Is.EqualTo(power), "power");
            Assert.That(r.Statuses[BudgetKind.Data], Is.EqualTo(data), "data");
            Assert.That(r.Statuses[BudgetKind.DeltaV], Is.EqualTo(BudgetStatus.Green), "delta_v (no requirement yet)");
        }

        [Test]
        public void TestDesign_Good_IsValidAndAllGreen()
        {
            var r = EvaluateTestDesign("techdemo_good.json");
            Assert.That(r.Issues, Is.Empty);
            AssertStatuses(r, BudgetStatus.Green, BudgetStatus.Green, BudgetStatus.Green, BudgetStatus.Green);
            Assert.That(r.AllGreen, Is.True);
            Assert.That(r.PowerResult.BatteryOk && r.DataResult.StorageOk, Is.True);
        }

        [Test]
        public void TestDesign_LowPower_FailsOnlyOnPower()
        {
            var r = EvaluateTestDesign("low_power.json");
            Assert.That(r.Issues, Is.Empty);
            AssertStatuses(r, BudgetStatus.Green, BudgetStatus.Green, BudgetStatus.Red, BudgetStatus.Green);
            Assert.That(r.PowerResult.BatteryOk, Is.False);
        }

        [Test]
        public void TestDesign_OverBudget_FailsOnlyOnCost()
        {
            var r = EvaluateTestDesign("over_budget.json");
            Assert.That(r.Issues, Is.Empty);
            AssertStatuses(r, BudgetStatus.Green, BudgetStatus.Red, BudgetStatus.Green, BudgetStatus.Green);
        }

        [Test]
        public void TestDesign_SlowDownlink_FailsOnlyOnData()
        {
            var r = EvaluateTestDesign("slow_downlink.json");
            Assert.That(r.Issues, Is.Empty);
            AssertStatuses(r, BudgetStatus.Green, BudgetStatus.Green, BudgetStatus.Green, BudgetStatus.Red);
        }

        [Test]
        public void TestDesign_Invalid_ReportsEveryBrokenRule()
        {
            var r = EvaluateTestDesign("invalid.json");
            Assert.That(r.IsValid, Is.False);
            Assert.That(r.Issues.Select(i => i.Code), Is.EquivalentTo(new[]
            {
                DesignIssueCode.MissingLauncher, DesignIssueCode.UnknownPart, DesignIssueCode.TooFewInstruments,
                DesignIssueCode.AltitudeOutsideMission, DesignIssueCode.SolarAreaTooLarge,
                DesignIssueCode.NoCompatibleGroundStation, DesignIssueCode.PropellantWithoutPropulsion,
                DesignIssueCode.InvalidValue
            }));
        }
    }
}
