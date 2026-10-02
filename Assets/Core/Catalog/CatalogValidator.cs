using System;
using System.Collections.Generic;

namespace MissionCore
{
    // Sanity checks for loaded data files. Every problem throws a FormatException
    // "<file>: '<id>' <problem>" so a broken JSON value is found right at startup.
    public static class CatalogValidator
    {
        const double SumTolerance = 1e-6;

        // ---------- Lists ----------

        public static void Validate(List<PropulsionSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, p => p.id, p => p.name, fileName);
            foreach (var p in list)
            {
                if (p.ispS <= 0) Fail(fileName, p.id, "has isp_s <= 0");
                if (p.thrustN <= 0) Fail(fileName, p.id, "has thrust_N <= 0");
                if (p.powerW.HasValue && p.powerW <= 0) Fail(fileName, p.id, "has power_W <= 0");
            }
        }

        public static void Validate(List<InstrumentSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, i => i.id, i => i.name, fileName);
            foreach (var i in list)
            {
                if (i.massKG <= 0) Fail(fileName, i.id, "has mass_kg <= 0");
                if (i.powerW < 0) Fail(fileName, i.id, "has power_W < 0");
            }
        }

        public static void Validate(List<LauncherSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, l => l.id, l => l.name, fileName);
            foreach (var l in list)
            {
                if (l.leoKG <= 0) Fail(fileName, l.id, "has leo_kg <= 0");
                if (l.ssoKG.HasValue && l.ssoKG <= 0) Fail(fileName, l.id, "has sso_kg <= 0");
                if (l.gtoKG.HasValue && l.gtoKG <= 0) Fail(fileName, l.id, "has gto_kg <= 0");
                if (l.successRate <= 0 || l.successRate > 1) Fail(fileName, l.id, "has success_rate outside (0, 1]");
                if (l.priceUSD <= 0) Fail(fileName, l.id, "has price_USD <= 0");
            }
        }

        public static void Validate(List<PlatformSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, p => p.id, p => p.name, fileName);
            foreach (var p in list)
            {
                if (p.busMassKG <= 0) Fail(fileName, p.id, "has bus_mass_kg <= 0");
                if (p.maxMassKG <= p.busMassKG) Fail(fileName, p.id, "has max_mass_kg <= bus_mass_kg");
                if (p.busPowerW < 0) Fail(fileName, p.id, "has bus_power_W < 0");
                if (p.maxSolarAreaM2 <= 0) Fail(fileName, p.id, "has max_solar_area_m2 <= 0");
                if (p.lifetimeYears <= 0) Fail(fileName, p.id, "has lifetime_years <= 0");
                if (p.priceUSD < 0) Fail(fileName, p.id, "has price_USD < 0");
            }
        }

        public static void Validate(List<SolarCellSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, s => s.id, s => s.name, fileName);
            foreach (var s in list)
            {
                if (s.efficiency <= 0 || s.efficiency >= 1) Fail(fileName, s.id, "has efficiency outside (0, 1)");
                if (s.packingFactor <= 0 || s.packingFactor > 1) Fail(fileName, s.id, "has packing_factor outside (0, 1]");
                if (s.massPerAreaKgM2 <= 0) Fail(fileName, s.id, "has mass_per_area_kg_m2 <= 0");
                if (s.degradationPerYear < 0 || s.degradationPerYear >= 1) Fail(fileName, s.id, "has degradation_per_year outside [0, 1)");
                if (s.pricePerAreaUSDM2 < 0) Fail(fileName, s.id, "has price_per_area_USD_m2 < 0");
            }
        }

        public static void Validate(List<BatterySpec> list, string fileName)
        {
            ValidateIdsAndNames(list, b => b.id, b => b.name, fileName);
            foreach (var b in list)
            {
                if (b.energyWh <= 0) Fail(fileName, b.id, "has energy_Wh <= 0");
                if (b.massKg <= 0) Fail(fileName, b.id, "has mass_kg <= 0");
                if (b.maxDepthOfDischarge <= 0 || b.maxDepthOfDischarge > 1) Fail(fileName, b.id, "has max_depth_of_discharge outside (0, 1]");
                if (b.priceUSD < 0) Fail(fileName, b.id, "has price_USD < 0");
            }
        }

        public static void Validate(List<CommsSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, c => c.id, c => c.name, fileName);
            foreach (var c in list)
            {
                if (string.IsNullOrEmpty(c.band)) Fail(fileName, c.id, "has no band");
                if (c.dataRateBpS <= 0) Fail(fileName, c.id, "has data_rate_bps <= 0");
                if (c.powerW < 0) Fail(fileName, c.id, "has power_W < 0");
                if (c.massKG <= 0) Fail(fileName, c.id, "has mass_kg <= 0");
                if (c.priceUSD < 0) Fail(fileName, c.id, "has price_USD < 0");
            }
        }

        public static void Validate(List<GroundStationSpec> list, string fileName)
        {
            ValidateIdsAndNames(list, g => g.id, g => g.name, fileName);
            foreach (var g in list)
            {
                if (g.latDeg < -90 || g.latDeg > 90) Fail(fileName, g.id, "has lat_deg outside [-90, 90]");
                if (g.lonDeg < -180 || g.lonDeg > 180) Fail(fileName, g.id, "has lon_deg outside [-180, 180]");
                if (g.bands == null || g.bands.Length == 0) Fail(fileName, g.id, "has no bands");
                foreach (string band in g.bands)
                    if (string.IsNullOrEmpty(band)) Fail(fileName, g.id, "has an empty band");
                if (g.minElevationDeg < 0 || g.minElevationDeg >= 90) Fail(fileName, g.id, "has min_elevation_deg outside [0, 90)");
                if (g.costPerContactUSD < 0) Fail(fileName, g.id, "has cost_per_contact_USD < 0");
            }
        }

        public static void Validate(List<MissionTemplate> list, string fileName)
        {
            // Missions have no "name"; the title text key takes its place.
            ValidateIdsAndNames(list, m => m.id, m => m.titleKey, fileName, "title_key");
            foreach (var m in list)
            {
                if (m.budgetUSD <= 0) Fail(fileName, m.id, "has budget_USD <= 0");
                if (m.durationDays <= 0) Fail(fileName, m.id, "has duration_days <= 0");
                if (m.minInstruments < 0) Fail(fileName, m.id, "has min_instruments < 0");

                if (m.altitudeM == null) Fail(fileName, m.id, "has no altitude_m");
                if (m.altitudeM.Min <= 0) Fail(fileName, m.id, "has altitude_m.min <= 0");
                if (m.altitudeM.Min > m.altitudeM.Max) Fail(fileName, m.id, "has altitude_m.min > altitude_m.max");

                if (m.requiredGoals == null || m.requiredGoals.Count == 0) Fail(fileName, m.id, "has no required_goals");
                ValidateGoals(m.requiredGoals, m.id, "required_goals", fileName);
                ValidateGoals(m.bonusGoals, m.id, "bonus_goals", fileName);

                if (m.fixedEvents == null) continue;
                foreach (var e in m.fixedEvents)
                {
                    string what = $"fixed event '{e.type.ToSnakeCase()}'";
                    if (e.day == null) Fail(fileName, m.id, $"{what} has no day range");
                    if (e.day.Min > e.day.Max) Fail(fileName, m.id, $"{what} has day.min > day.max");
                    if (e.day.Min < 0 || e.day.Max > m.durationDays) Fail(fileName, m.id, $"{what} lies outside the mission duration");
                }
            }
        }

        static void ValidateGoals(List<Goal> goals, string missionId, string listName, string fileName)
        {
            if (goals == null) return;
            foreach (var g in goals)
            {
                string what = $"{listName} '{g.type.ToSnakeCase()}'";
                if (NeedsValue(g.type) && !g.value.HasValue) Fail(fileName, missionId, $"{what} has no value");
                if (g.value < 0) Fail(fileName, missionId, $"{what} has a negative value");
            }
        }

        // Goal types that are measured against a number. New types without a number return false.
        static bool NeedsValue(GoalType type)
        {
            switch (type)
            {
                case GoalType.SurviveDays:
                case GoalType.DataDownlinkedBytes:
                case GoalType.MaxCostUsd:
                    return true;
                default:
                    return false;
            }
        }

        // ---------- Single objects ----------

        public static void Validate(BalanceRules rules, string fileName)
        {
            var f = rules.DryMassFractions;
            if (f == null) Fail(fileName, "has no dry_mass_fractions_earth_orbit_with_propulsion");
            double[] fractions = { f.Payload, f.Structure, f.Thermal, f.Power, f.Communications,
                                   f.AttitudeControl, f.Computer, f.Propulsion, f.Other };
            double sum = 0;
            foreach (double x in fractions)
            {
                if (x < 0) Fail(fileName, "has a negative dry mass fraction");
                sum += x;
            }
            if (Math.Abs(sum - 1) > SumTolerance) Fail(fileName, $"dry mass fractions sum to {sum}, not 1");
            if (rules.MassMarginEarlyPhase < 0) Fail(fileName, "has mass_margin_early_phase < 0");
            if (rules.PowerMarginEarlyPhase < 0) Fail(fileName, "has power_margin_early_phase < 0");
        }

        public static void Validate(ScoringRules rules, string fileName)
        {
            var w = rules.Weights;
            if (w == null) Fail(fileName, "has no weights");
            if (w.Data < 0 || w.Budget < 0 || w.Safety < 0) Fail(fileName, "has a negative weight");
            double sum = w.Data + w.Budget + w.Safety;
            if (Math.Abs(sum - 1) > SumTolerance) Fail(fileName, $"weights sum to {sum}, not 1");
            if (rules.DataCapRatio < 1) Fail(fileName, "has data_cap_ratio < 1");
            if (rules.Stars == null) Fail(fileName, "has no stars");
            if (rules.Stars.Max < 1) Fail(fileName, "has stars.max < 1");
        }

        public static void Validate(SimRules rules, string fileName)
        {
            if (rules.TickS <= 0) Fail(fileName, "has tick_s <= 0");
            if (rules.IntegratorSubsteps < 1) Fail(fileName, "has integrator_substeps < 1");

            var ls = rules.LoadShedding;
            if (ls == null) Fail(fileName, "has no load_shedding");
            foreach (double soc in new[] { ls.SafeModeBelowSoc, ls.InstrumentsOffBelowSoc, ls.InstrumentsOnAboveSoc })
                if (soc <= 0 || soc >= 1) Fail(fileName, "has a load_shedding threshold outside (0, 1)");
            // Hysteresis: safe mode kicks in last, instruments come back only well above the off threshold.
            if (!(ls.SafeModeBelowSoc < ls.InstrumentsOffBelowSoc && ls.InstrumentsOffBelowSoc < ls.InstrumentsOnAboveSoc))
                Fail(fileName, "needs safe_mode_below_soc < instruments_off_below_soc < instruments_on_above_soc");

            var density = rules.SolarActivityDensityFactor;
            if (density == null) Fail(fileName, "has no solar_activity_density_factor");
            foreach (SolarActivity activity in Enum.GetValues(typeof(SolarActivity)))
            {
                string key = activity.ToSnakeCase();
                if (!density.TryGetValue(key, out double factor)) Fail(fileName, $"has no solar_activity_density_factor for '{key}'");
                if (factor <= 0) Fail(fileName, $"has solar_activity_density_factor '{key}' <= 0");
            }
        }

        // ---------- Texts ----------

        public static void Validate(Dictionary<string, Dictionary<string, string>> texts, string fileName)
        {
            foreach (var entry in texts)
            {
                if (entry.Value == null || !entry.Value.TryGetValue("en", out string en) || string.IsNullOrEmpty(en))
                    Fail(fileName, entry.Key, "has no \"en\" text");
            }
        }

        // ---------- Helpers ----------

        static void ValidateIdsAndNames<T>(List<T> list, Func<T, string> getId, Func<T, string> getName,
                                           string fileName, string nameField = "name")
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) throw new FormatException($"{fileName}: entry {i} is null");
                string id = getId(list[i]);
                if (string.IsNullOrEmpty(id)) throw new FormatException($"{fileName}: entry {i} has no id");
                if (!ids.Add(id)) Fail(fileName, id, "is a duplicate id");
                if (string.IsNullOrEmpty(getName(list[i]))) Fail(fileName, id, $"has no {nameField}");
            }
        }

        static void Fail(string fileName, string id, string problem) =>
            throw new FormatException($"{fileName}: '{id}' {problem}");

        static void Fail(string fileName, string problem) =>
            throw new FormatException($"{fileName}: {problem}");
    }
}
