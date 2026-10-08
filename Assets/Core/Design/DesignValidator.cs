using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionCore
{
    public static class DesignValidator
    {
        public static List<DesignIssue> Validate(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> requiredPartsIssues = ValidateRequiredParts(design);
            List<DesignIssue> existingPartsIssues = ValidateExistingParts(catalog, design);
            List<DesignIssue> missionIssues = ValidateMissionNeeds(catalog, design);
            List<DesignIssue> orbitAndRocketIssues = ValidateOrbitAndRocketSpecs(catalog, design);
            List<DesignIssue> platformBoundariesIssues = ValidatePlatformBoundaries(catalog, design);
            List<DesignIssue> communicationIssues = ValidateCommunication(catalog, design);
            List<DesignIssue> numbersIssues = ValidateNumbers(design);

            return new List<DesignIssue>()
                .Concat(requiredPartsIssues)
                .Concat(existingPartsIssues)
                .Concat(missionIssues)
                .Concat(orbitAndRocketIssues)
                .Concat(platformBoundariesIssues)
                .Concat(communicationIssues)
                .Concat(numbersIssues)
                .ToList();
        }

        private static List<DesignIssue> ValidateOrbitAndRocketSpecs(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> issues = new();

            if (design.InclinationDeg < 0 || design.InclinationDeg > 180)
                issues.Add(new(DesignIssueCode.InvalidInclination, FormattableString.Invariant($"{design.InclinationDeg:0.#}°")));

            LaunchOrbit? orbit = OrbitClassifier.Classify(design.AltitudeM, design.InclinationDeg);
            if (orbit == null)
            {
                issues.Add(new(DesignIssueCode.OrbitNotSupported, $"{design.AltitudeM / 1000:0} km"));
                return issues;   // without an orbit class the launcher cannot be checked
            }

            if (!IdIsSet(design.LauncherId) || !catalog.Launchers.TryGetValue(design.LauncherId, out LauncherSpec launcher))
                return issues;   // MissingLauncher / UnknownPart already reported

            if (launcher.IsRideshare)
            {
                if (launcher.orbits == null || !launcher.orbits.Contains(orbit.Value))
                    issues.Add(new(DesignIssueCode.RideshareOrbitMismatch, orbit.Value.ToSnakeCase()));
                if (launcher.altitudeM != null && !launcher.altitudeM.Contains(design.AltitudeM))
                    issues.Add(new(DesignIssueCode.RideshareAltitudeMismatch, FormattableString.Invariant($"{design.AltitudeM / 1000:0} km ({launcher.altitudeM.Min / 1000:0}–{launcher.altitudeM.Max / 1000:0} km)")));
            }
            else if (orbit == LaunchOrbit.Gto && launcher.gtoKg == null)
            {
                issues.Add(new(DesignIssueCode.LauncherCannotReachOrbit, orbit.Value.ToSnakeCase()));
            }
            return issues;
        }

        private static List<DesignIssue> ValidatePlatformBoundaries(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> issues = new();

            if (!IdIsSet(design.PlatformId) || !catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platform))
                return issues;

            if (platform.maxSolarAreaM2 < design.SolarAreaM2) issues.Add(new(DesignIssueCode.SolarAreaTooLarge, FormattableString.Invariant($"{design.SolarAreaM2:0.###} m² (max {platform.maxSolarAreaM2:0.###} m²)")));

            if (!IdIsSet(design.MissionId) || !catalog.Missions.TryGetValue(design.MissionId, out MissionTemplate mission))
                return issues;

            if (platform.lifetimeYears < mission.durationDays/Constants.DaysPerYear) issues.Add(new(DesignIssueCode.MissionLongerThanLifetime, FormattableString.Invariant($"{mission.durationDays:0} d (max {platform.lifetimeYears * Constants.DaysPerYear:0} d)")));

            return issues;
        }

        private static List<DesignIssue> ValidateCommunication(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> issues = new();

            if (!IdIsSet(design.CommsId) || !catalog.Comms.TryGetValue(design.CommsId, out CommsSpec comms))
                return issues;

            bool compatibleGroundStationFound = false;
            if (design.GroundStationIds == null || design.GroundStationIds.Count <= 0)
                return issues;
            foreach(string groundStationId in design.GroundStationIds ?? new())
            {
                if (!IdIsSet(groundStationId) || !catalog.GroundStations.TryGetValue(groundStationId, out GroundStationSpec groundStation))
                    continue;

                if (groundStation.bands.Contains(comms.band, StringComparer.OrdinalIgnoreCase)) compatibleGroundStationFound = true;
            }
            if (!compatibleGroundStationFound) issues.Add(new(DesignIssueCode.NoCompatibleGroundStation, comms.band));

            return issues;
        }

        private static List<DesignIssue> ValidateNumbers(MissionDesign design)
        {
            List<DesignIssue> issues = new();

            if (!IdIsSet(design.PropulsionId))
                if (design.PropellantMassKg > 0) issues.Add(new(DesignIssueCode.PropellantWithoutPropulsion, FormattableString.Invariant($"{design.PropellantMassKg:0.##} kg")));

            if (!double.IsFinite(design.AltitudeM) || design.AltitudeM <= 0) issues.Add(new(DesignIssueCode.InvalidValue, "altitude_m"));
            if (!double.IsFinite(design.SolarAreaM2) || design.SolarAreaM2 < 0) issues.Add(new(DesignIssueCode.InvalidValue, "solar_area_m2"));
            if (!double.IsFinite(design.PropellantMassKg) || design.PropellantMassKg < 0) issues.Add(new(DesignIssueCode.InvalidValue, "propellant_mass_kg"));
            if (!double.IsFinite(design.BatteryCount) || design.BatteryCount < 1) issues.Add(new(DesignIssueCode.InvalidValue, "battery_count"));

            return issues;
        }

        private static List<DesignIssue> ValidateMissionNeeds(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> issues = new();
            if (IdIsSet(design.MissionId) && catalog.Missions.TryGetValue(design.MissionId, out MissionTemplate mission))
            {
                if (design.InstrumentIds != null && mission.minInstruments > ValidInstrumentCount(catalog, design.InstrumentIds)) issues.Add(new(DesignIssueCode.TooFewInstruments, FormattableString.Invariant($"{design.InstrumentIds.Count}/{mission.minInstruments}")));
                if (!mission.altitudeM.Contains(design.AltitudeM)) issues.Add(new(DesignIssueCode.AltitudeOutsideMission, FormattableString.Invariant($"{design.AltitudeM / 1000:0} km ({mission.altitudeM.Min / 1000:0}–{mission.altitudeM.Max / 1000:0} km)")));
            }
            return issues;
        }

        private static int ValidInstrumentCount(Catalog catalog, List<string> instrumentIds)
        {
            int validInstruments = 0;

            instrumentIds.ForEach(instrumentId => 
                { if (IdIsValid(catalog.Instruments, instrumentId)) validInstruments++; }
            );

            return validInstruments;
        }

        private static List<DesignIssue> ValidateExistingParts(Catalog catalog, MissionDesign design)
        {
            

            List<DesignIssue> issues = new();

            if (IdIsUnknown(catalog.Platforms, design.PlatformId)) issues.Add(new(DesignIssueCode.UnknownPart, design.PlatformId));
            if (IdIsUnknown(catalog.Missions, design.MissionId)) issues.Add(new(DesignIssueCode.UnknownPart, design.MissionId));
            if (IdIsUnknown(catalog.Launchers, design.LauncherId)) issues.Add(new(DesignIssueCode.UnknownPart, design.LauncherId));
            if (IdIsUnknown(catalog.Comms, design.CommsId)) issues.Add(new(DesignIssueCode.UnknownPart, design.CommsId));
            if (IdIsUnknown(catalog.Batteries, design.BatteryId)) issues.Add(new(DesignIssueCode.UnknownPart, design.BatteryId));
            if (IdIsUnknown(catalog.SolarCells, design.SolarCellId)) issues.Add(new(DesignIssueCode.UnknownPart, design.SolarCellId));
            if (IdIsUnknown(catalog.Propulsion, design.PropulsionId)) issues.Add(new(DesignIssueCode.UnknownPart, design.PropulsionId));

            foreach (string instrumentId in design.InstrumentIds ?? new List<string>())
                if (IdIsUnknown(catalog.Instruments, instrumentId)) issues.Add(new(DesignIssueCode.UnknownPart, instrumentId));
            foreach (string groundStationId in design.GroundStationIds ?? new List<string>())
                if (IdIsUnknown(catalog.GroundStations, groundStationId)) issues.Add(new(DesignIssueCode.UnknownPart, groundStationId));

            return issues;
        }

        private static List<DesignIssue> ValidateRequiredParts(MissionDesign design)
        {
            List<DesignIssue> issues = new();

            if (!IdIsSet(design.PlatformId)) issues.Add(new(DesignIssueCode.MissingPlatform));
            if (!IdIsSet(design.MissionId)) issues.Add(new(DesignIssueCode.MissingMission));
            if (!IdIsSet(design.LauncherId)) issues.Add(new(DesignIssueCode.MissingLauncher));
            if (!IdIsSet(design.CommsId)) issues.Add(new(DesignIssueCode.MissingComms));
            if (!IdIsSet(design.BatteryId)) issues.Add(new(DesignIssueCode.MissingBattery));
            if (!IdIsSet(design.SolarCellId)) issues.Add(new(DesignIssueCode.MissingSolarCell));
            if (design.GroundStationIds == null || design.GroundStationIds.Count == 0) issues.Add(new(DesignIssueCode.NoGroundStation));

            return issues;
        }

        private static bool IdIsSet(string id)
        {
            return !string.IsNullOrEmpty(id);
        }
        private static bool IdIsUnknown<T>(IReadOnlyDictionary<string, T> parts, string id)
        {
            return IdIsSet(id) && !parts.ContainsKey(id);
        }
        private static bool IdIsValid<T>(IReadOnlyDictionary<string, T> parts, string id)
        {
            return IdIsSet(id) && parts.ContainsKey(id);
        }

        public static DesignReport Evaluate(Catalog catalog, MissionDesign design)
        {
            List<DesignIssue> issues = Validate(catalog, design);

            MassBudget massBudget = new();
            CostBudget costBudget = new();
            DeltaVBudget deltaVBudget = new();
            PowerBudget powerBudget = new();
            DataBudget dataBudget = new();

            var ctx = new BudgetContext(catalog, design);
            ctx.Mass  = massBudget.Evaluate(ctx);      // needs nothing
            var massResult  = ctx.Mass;
            var costResult  = costBudget.Evaluate(ctx);      // uses ctx.Mass.TotalMassKg for the launch price
            var deltaVResult    = deltaVBudget.Evaluate(ctx);    // uses ctx.Mass for m0 and m1
            var powerResult = powerBudget.Evaluate(ctx);
            var dataResult  = dataBudget.Evaluate(ctx);

            Dictionary<BudgetKind, BudgetStatus> statuses = new()
            {
                { BudgetKind.Mass, catalog.Balance.StatusFor(BudgetKind.Mass, massResult.ReserveFraction) },
                { BudgetKind.Cost, catalog.Balance.StatusFor(BudgetKind.Cost, costResult.ReserveFraction) },
                { BudgetKind.DeltaV, deltaVResult.RequiredDeltaVMPerS == 0 ? BudgetStatus.Green : catalog.Balance.StatusFor(BudgetKind.DeltaV, deltaVResult.ReserveFraction) },
                { BudgetKind.Power, catalog.Balance.StatusFor(BudgetKind.Power, powerResult.ReserveFraction) },
                { BudgetKind.Data, catalog.Balance.StatusFor(BudgetKind.Data, dataResult.ReserveFraction) },
            };

            return new(
                massResult,
                costResult,
                deltaVResult,
                powerResult,
                dataResult,
                statuses,
                issues
            );
        }
    }
}