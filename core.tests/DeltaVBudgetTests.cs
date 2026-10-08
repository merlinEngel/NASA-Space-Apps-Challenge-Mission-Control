using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class DeltaVBudgetTests
    {
        const double Tol = 1e-6;

        static Catalog TestCatalog() => new Catalog(
            new List<PropulsionSpec>
            {
                new PropulsionSpec { id = "hydrazine", ispS = 225, dryMassKg = 5 },
                new PropulsionSpec { id = "ion", ispS = 3100, dryMassKg = 10 },
            },
            new List<InstrumentSpec>(), new List<LauncherSpec>(), new List<CommsSpec>(), new List<BatterySpec>(),
            new List<PlatformSpec> { new PlatformSpec { id = "smallsat", busMassKg = 35, maxMassKg = 150 } },
            new List<GroundStationSpec>(), new List<SolarCellSpec>(), new List<MissionTemplate>(),
            new BalanceRules { MassMarginEarlyPhase = 0.25 }, null, null, null, new List<OrbitPresetSpec>());

        static MissionDesign Design(string propulsionId, double propellantKg) => new MissionDesign
        {
            PlatformId = "smallsat", PropulsionId = propulsionId, PropellantMassKg = propellantKg
        };

        // dry = bus 35 + hydrazine 5 = 40 kg, with margin 40 x 1.25 = 50 kg, wet = 50 + 10 = 60 kg
        // dv = 225 s x 9.80665 m/s2 x ln(60 / 50) = 2,206.496 x 0.1823216 = 402.29 m/s
        [Test]
        public void Hydrazine_MatchesRocketEquation()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), Design("hydrazine", 10));
            Assert.That(r.IspS, Is.EqualTo(225));
            Assert.That(r.WetMassKg, Is.EqualTo(60).Within(Tol));
            Assert.That(r.EmptyMassKg, Is.EqualTo(50).Within(Tol));
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(225 * 9.80665 * Math.Log(60.0 / 50.0)).Within(Tol));
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(402.29).Within(0.01));
        }

        // Margin counts as dry mass: without it the result would be 225 x 9.80665 x ln(50 / 40) = 492.4 m/s
        [Test]
        public void MarginIsPartOfDryMass()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), Design("hydrazine", 10));
            Assert.That(r.AvailableDeltaVMPerS, Is.LessThan(225 * 9.80665 * Math.Log(50.0 / 40.0)));
        }

        // dry = 35 + 10 = 45 kg, with margin 56.25 kg, wet = 76.25 kg
        // dv = 3100 x 9.80665 x ln(76.25 / 56.25) = 9,251.6 m/s
        [Test]
        public void IonEngine_GivesMuchMoreDeltaVPerKg()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), Design("ion", 20));
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(3100 * 9.80665 * Math.Log(76.25 / 56.25)).Within(Tol));
        }

        [Test]
        public void NoPropulsion_GivesZero()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), Design(null, 10));
            Assert.That(r.IspS, Is.EqualTo(0));
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(0));
        }

        [Test]
        public void NoPropellant_GivesZero()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), Design("hydrazine", 0));
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(0));
        }

        [Test]
        public void RequiredDeltaV_IsZeroForNow()
        {
            Assert.That(DeltaVBudget.Evaluate(TestCatalog(), Design("hydrazine", 10)).RequiredDeltaVMPerS, Is.EqualTo(0));
        }

        [Test]
        public void EmptyDesign_HasNoNaN()
        {
            var r = DeltaVBudget.Evaluate(TestCatalog(), new MissionDesign());
            Assert.That(r.AvailableDeltaVMPerS, Is.EqualTo(0));
            Assert.That(r.ReserveMPerS, Is.EqualTo(0));
        }

        [Test]
        public void ReserveFraction_WithoutRequirement_IsZero()
        {
            Assert.That(DeltaVBudget.Evaluate(TestCatalog(), Design("hydrazine", 10)).ReserveFraction, Is.EqualTo(0));
        }
    }
}
