using System;
using System.Collections.Generic;
using System.Linq;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Rules;
using MissionCore.Simulation;
using static MissionCore.Numerics.CoreConstants;

namespace MissionCore.Design
{
    // Flight readiness review: may this design launch? Red items block the launch, yellow items are risks
    // the player confirms. All numbers come from the design report; this class only judges them.
    public static class FlightReadinessReview
    {
        public static ReviewResult Review(Catalog catalog, MissionDesign design)
        {
            DesignReport report = DesignValidator.Evaluate(catalog, design);
            if (!report.IsValid)
                throw new ArgumentException("Review needs a valid design, issues: " + string.Join(", ", report.Issues.Select(i => i.Code)));

            ReviewRules rules = catalog.Balance.Review
                ?? throw new InvalidOperationException("balance_rules.json has no review group");
            ScoringRules scoring = catalog.Scoring
                ?? throw new InvalidOperationException("Review needs scoring.json (disposal_max_years)");

            double missionS = report.LifetimeResult.MissionDurationS;

            // Same order as ReviewCode
            var items = new List<ReviewItem>
            {
                CheckLaunchMass(report),
                CheckBudget(report),
                CheckInstruments(catalog, design),
                CheckGroundStations(catalog, design),
                CheckPowerReserve(report, rules),
                CheckStorage(report),
                CheckAvoidance(report, design),
                CheckDisposal(report, design, rules, scoring),
                CheckLifetime(report)
            };
            return new ReviewResult(report, items);
        }

        // --- Red ---

        static ReviewItem CheckLaunchMass(DesignReport report)
        {
            MassBudgetResult mass = report.MassResult;
            return new ReviewItem(ReviewCode.LaunchMassTooHigh, RedIf(mass.TotalMassKg > mass.LimitKg), mass.TotalMassKg, mass.LimitKg);
        }

        static ReviewItem CheckBudget(DesignReport report)
        {
            CostBudgetResult cost = report.CostResult;
            return new ReviewItem(ReviewCode.BudgetExceeded, RedIf(cost.TotalCostUSD > cost.BudgetUSD), cost.TotalCostUSD, cost.BudgetUSD);
        }

        // Display only: the validator already rejects too few instruments, so this is always green here.
        static ReviewItem CheckInstruments(Catalog catalog, MissionDesign design) =>
            new ReviewItem(ReviewCode.RequiredInstrumentMissing, BudgetStatus.Green,
                design.InstrumentIds.Count, catalog.Missions[design.MissionId].minInstruments);

        // Display only: the validator already rejects a design without a compatible station.
        static ReviewItem CheckGroundStations(Catalog catalog, MissionDesign design) =>
            new ReviewItem(ReviewCode.NoGroundStation, BudgetStatus.Green,
                design.GroundStationIds.Count, detail: catalog.Comms[design.CommsId].band);

        // --- Yellow ---

        static ReviewItem CheckPowerReserve(DesignReport report, ReviewRules rules)
        {
            PowerBudgetResult power = report.PowerResult;
            bool low = power.ReserveFraction < rules.MinPowerReserve || !power.BatteryOk;
            return new ReviewItem(ReviewCode.LowPowerReserve, YellowIf(low), power.ReserveFraction, rules.MinPowerReserve,
                power.BatteryOk ? null : "battery");
        }

        static ReviewItem CheckStorage(DesignReport report)
        {
            DataBudgetResult data = report.DataResult;
            double missionDays = report.LifetimeResult.MissionDurationS / SecondsPerDay;

            double daysUntilFull;
            if (!data.StorageOk) daysUntilFull = 0;   // a single contact gap is already too long
            else if (data.GeneratedBitsPerDay > data.DownlinkBitsPerDay)
                daysUntilFull = data.StorageCapacityBits / (data.GeneratedBitsPerDay - data.DownlinkBitsPerDay);
            else daysUntilFull = double.PositiveInfinity;

            return new ReviewItem(ReviewCode.StorageFillsUp, YellowIf(daysUntilFull < missionDays), daysUntilFull, missionDays);
        }

        static ReviewItem CheckAvoidance(DesignReport report, MissionDesign design)
        {
            double spare = report.DeltaVResult.AfterDragMPerS;
            double needed = report.DeltaVResult.AvoidanceMPerS;
            return new ReviewItem(ReviewCode.NoAvoidancePropellant, YellowIf(spare < needed), spare, needed,
                string.IsNullOrEmpty(design.PropulsionId) ? "no_propulsion" : null);
        }

        static ReviewItem CheckDisposal(DesignReport report, MissionDesign design, ReviewRules rules, ScoringRules scoring)
        {
            double restS = report.LifetimeResult.LifetimeS - report.LifetimeResult.MissionDurationS;
            double restYears = restS / (DaysPerYear * SecondsPerDay);

            string detail = FormattableString.Invariant($"{restYears:0.0}");
            double left = Math.Max(0, report.DeltaVResult.AfterAvoidanceMPerS);

            if (restYears <= scoring.DisposalMaxYears)
                return new ReviewItem(ReviewCode.NoDisposalPropellant, BudgetStatus.Green, left, 0, detail);

            double needed = report.DeltaVResult.DisposalMPerS;
            return new ReviewItem(ReviewCode.NoDisposalPropellant, YellowIf(left < needed), left, needed, detail);
        }

        static ReviewItem CheckLifetime(DesignReport report)
        {
            LifetimeBudgetResult life = report.LifetimeResult;
            return new ReviewItem(ReviewCode.LifetimeShorterThanMission, YellowIf(life.LifetimeS < life.MissionDurationS),
                life.LifetimeS, life.MissionDurationS);
        }

        // --- Helpers ---
        static BudgetStatus RedIf(bool condition) => condition ? BudgetStatus.Red : BudgetStatus.Green;
        static BudgetStatus YellowIf(bool condition) => condition ? BudgetStatus.Yellow : BudgetStatus.Green;
    }
}
