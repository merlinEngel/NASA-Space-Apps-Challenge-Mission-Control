using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MissionCore.Bodies;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;

namespace MissionCore.Tests
{
    // P5 step 6: Earth's shadow, beta angle and eclipse fraction
    public class ShadowTests
    {
        const double Deg = Math.PI / 180;
        const double R = Constants.RadiusEarth;
        static readonly double TechDemoEpoch = (new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc) - Constants.J2000DateTime).TotalSeconds;

        // ---------- IsInShadow ----------

        [Test]
        public void IsInShadow_BehindEarth_True() =>
            Assert.That(Shadow.IsInShadow(new Vec3d(-(R + 400e3), 0, 0), Vec3d.UnitX), Is.True);

        [Test]
        public void IsInShadow_DaySide_False() =>
            Assert.That(Shadow.IsInShadow(new Vec3d(R + 400e3, 0, 0), Vec3d.UnitX), Is.False);

        [Test]
        public void IsInShadow_BehindButBesideEarth_False() =>
            Assert.That(Shadow.IsInShadow(new Vec3d(-(R + 400e3), 1.1 * R, 0), Vec3d.UnitX), Is.False);

        [Test]
        public void IsInShadow_AtTerminator_False() =>
            Assert.That(Shadow.IsInShadow(new Vec3d(0, R + 400e3, 0), Vec3d.UnitX), Is.False);

        // ---------- BetaAngle ----------

        [Test]
        public void BetaAngle_EquatorialOrbit_SunAtEquinox_IsZero() =>
            Assert.That(Shadow.BetaAngle(0, 0, Vec3d.UnitX), Is.EqualTo(0).Within(1e-12));

        [Test]
        public void BetaAngle_PolarOrbitFacingSun_Is90()
        {
            // i = 90°, raan = 90°: orbit normal points along +x, straight at the Sun
            Assert.That(Shadow.BetaAngle(90 * Deg, 90 * Deg, Vec3d.UnitX) / Deg, Is.EqualTo(90).Within(1e-6));
            Assert.That(Shadow.BetaAngle(90 * Deg, 270 * Deg, Vec3d.UnitX) / Deg, Is.EqualTo(-90).Within(1e-6));
        }

        [Test]
        public void BetaAngle_NormalMatchesAngularMomentumOfKeplerOrbit()
        {
            // The orbit normal used for beta must point where r x v points
            double inc = 51.6 * Deg, raan = 123 * Deg;
            var orbit = KeplerOrbit.Circular(400e3, inc, raan, R, Constants.MuEarth, 0);
            var (r, v) = orbit.StateAt(1234);
            Vec3d h = r.Cross(v).Normalized;
            foreach (Vec3d sun in new[] { Vec3d.UnitX, Vec3d.UnitY, Vec3d.UnitZ, new Vec3d(1, -2, 0.5).Normalized })
                Assert.That(Shadow.BetaAngle(inc, raan, sun), Is.EqualTo(Math.Asin(h.Dot(sun))).Within(1e-9), sun.ToString());
        }

        // ---------- EclipseFraction ----------

        [Test]
        public void EclipseFraction_BetaZero_EqualsOldFormula()
        {
            double r = R + 400e3;
            Assert.That(Shadow.EclipseFraction(400e3, 0), Is.EqualTo(Math.Asin(R / r) / Math.PI).Within(1e-9));
            Assert.That(Shadow.EclipseFraction(400e3, 0), Is.EqualTo(0.3901).Within(0.0005));
        }

        [TestCase(30, 0.3722)]
        [TestCase(60, 0.2633)]
        [TestCase(-60, 0.2633)]
        [TestCase(70, 0.0461)]
        [TestCase(75, 0.0)]
        [TestCase(90, 0.0)]
        public void EclipseFraction_400Km(double betaDeg, double expected)
        {
            double f = Shadow.EclipseFraction(400e3, betaDeg * Deg);
            Assert.That(double.IsNaN(f), Is.False);
            Assert.That(f, Is.EqualTo(expected).Within(0.0005));
        }

        [Test]
        public void EclipseFraction_FallsWithBeta()
        {
            double previous = 1;
            for (double b = 0; b <= 70; b += 5)
            {
                double f = Shadow.EclipseFraction(500e3, b * Deg);
                Assert.That(f, Is.LessThanOrEqualTo(previous), $"beta {b}");
                previous = f;
            }
        }

        // ---------- WorstEclipseFraction ----------

        [Test]
        public void WorstEclipseFraction_TechDemoSso()
        {
            double f = Shadow.WorstEclipseFraction(500e3, 97.5 * Deg, 0, TechDemoEpoch, 90 * 86400);
            Assert.That(f, Is.EqualTo(0.164).Within(0.003));
        }

        [Test]
        public void WorstEclipseFraction_IssOrbit_ReachesBetaZero()
        {
            double f = Shadow.WorstEclipseFraction(400e3, 51.6 * Deg, 0, TechDemoEpoch, 90 * 86400);
            Assert.That(f, Is.EqualTo(0.390).Within(0.002));
        }

        [Test]
        public void WorstEclipseFraction_NeverAboveBetaZeroValue()
        {
            double f = Shadow.WorstEclipseFraction(600e3, 30 * Deg, 1, TechDemoEpoch, 365 * 86400);
            Assert.That(f, Is.LessThanOrEqualTo(Shadow.EclipseFraction(600e3, 0) + 1e-12));
        }

        [Test]
        public void WorstEclipseFraction_ShortMission_DoesNotCrash()
        {
            double f = Shadow.WorstEclipseFraction(400e3, 51.6 * Deg, 0, TechDemoEpoch, 3600);
            Assert.That(f, Is.InRange(0.0, 0.5));
            Assert.That(Shadow.WorstEclipseFraction(400e3, 51.6 * Deg, 0, TechDemoEpoch, 0), Is.InRange(0.0, 0.5));
        }

        // ---------- PowerBudget ----------

        static Catalog RealCatalog()
        {
            string data = TestHelpers.StreamingDataDir();
            List<T> L<T>(string f) => CatalogLoader.LoadList<T>(File.ReadAllText(Path.Combine(data, f)), f);
            return new Catalog(L<PropulsionSpec>("propulsion.json"), L<InstrumentSpec>("instruments.json"),
                L<LauncherSpec>("launchers.json"), L<CommsSpec>("comms.json"), L<BatterySpec>("batteries.json"),
                L<PlatformSpec>("platforms.json"), L<GroundStationSpec>("groundstations.json"), L<SolarCellSpec>("solar.json"),
                L<MissionTemplate>("missions.json"),
                TestHelpers.LoadRealBalanceRules(),
                null, null, null, new List<OrbitPresetSpec>());
        }

        static MissionDesign TechDemo()
        {
            string root = Directory.GetParent(TestHelpers.StreamingDataDir()).Parent.Parent.FullName;
            string file = Path.Combine(root, "core.tests", "Data", "designs", "techdemo_good.json");
            return CatalogLoader.LoadObject<MissionDesign>(File.ReadAllText(file), "techdemo_good.json");
        }

        static PowerBudgetResult Power(MissionDesign design)
        {
            var ctx = new BudgetContext(RealCatalog(), design);
            ctx.Mass = new MassBudget().Evaluate(ctx);
            return new PowerBudget().Evaluate(ctx);
        }

        [Test]
        public void PowerBudget_TechDemo_UsesRealEclipse()
        {
            MissionDesign design = TechDemo();
            Assert.That(design.InclinationDeg, Is.EqualTo(97.5));
            Assert.That(Power(design).EclipseFraction, Is.EqualTo(0.164).Within(0.003));
        }

        [Test]
        public void PowerBudget_WithoutMission_UsesWorstCase()
        {
            MissionDesign design = TechDemo();
            design.MissionId = null;
            double r = R + design.AltitudeM;
            Assert.That(Power(design).EclipseFraction, Is.EqualTo(Math.Asin(R / r) / Math.PI).Within(1e-9));
        }
    }
}
