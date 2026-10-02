using System;
using System.Collections.Generic;

namespace MissionCore
{
    public class Catalog
    {
        public IReadOnlyDictionary<string, PropulsionSpec> Propulsion { get; }
        public IReadOnlyDictionary<string, InstrumentSpec> Instruments { get; }
        public IReadOnlyDictionary<string, LauncherSpec> Launchers { get; }
        public IReadOnlyDictionary<string, CommsSpec> Comms { get; }
        public IReadOnlyDictionary<string, BatterySpec> Batteries { get; }
        public IReadOnlyDictionary<string, PlatformSpec> Platforms { get; }
        public IReadOnlyDictionary<string, GroundStationSpec> GroundStations { get; }
        public IReadOnlyDictionary<string, SolarCellSpec> Solar { get; }

        public IReadOnlyDictionary<string, MissionTemplate> Missions { get; }

        public BalanceRules Balance { get; }
        public ScoringRules Scoring { get; }   // null while scoring.json does not exist
        public SimRules Sim { get; }           // null while sim_rules.json does not exist
        public TextTable Texts { get; }        // null while texts.json does not exist

        // Problems that do not stop the game (e.g. missing part texts), filled by ValidateReferences().
        public IReadOnlyList<string> Warnings { get; }

        public Catalog(List<PropulsionSpec> propulsion, List<InstrumentSpec> instruments,
                       List<LauncherSpec> launchers, List<CommsSpec> comms,
                       List<BatterySpec> batteries, List<PlatformSpec> platforms,
                       List<GroundStationSpec> groundStations, List<SolarCellSpec> solar,
                       List<MissionTemplate> missions,
                       BalanceRules balance, ScoringRules scoring, SimRules sim, TextTable texts)
        {
            Propulsion = ById(propulsion, p => p.id, "propulsion.json");
            Instruments = ById(instruments, i => i.id, "instruments.json");
            Launchers = ById(launchers, l => l.id, "launchers.json");
            Comms = ById(comms, c => c.id, "comms.json");
            Batteries = ById(batteries, b => b.id, "batteries.json");
            Platforms = ById(platforms, p => p.id, "platforms.json");
            GroundStations = ById(groundStations, g => g.id, "groundstations.json");
            Solar = ById(solar, s => s.id, "solar.json");
            Missions = ById(missions, m => m.id, "missions.json");

            Balance = balance;
            Scoring = scoring;
            Sim = sim;
            Texts = texts;

            Warnings = ValidateReferences();
        }

        // Checks links between files. Broken goal targets throw; missing texts only produce warnings
        // because texts.json is still incomplete.
        public List<string> ValidateReferences()
        {
            foreach (var m in Missions.Values)
            {
                CheckGoalTargets(m.requiredGoals, m.id);
                CheckGoalTargets(m.bonusGoals, m.id);
            }

            var warnings = new List<string>();
            if (Texts == null)
            {
                warnings.Add("texts.json not loaded, part names will show as keys");
                return warnings;
            }

            var partIds = new List<string>();
            partIds.AddRange(Propulsion.Keys);
            partIds.AddRange(Instruments.Keys);
            partIds.AddRange(Launchers.Keys);
            partIds.AddRange(Comms.Keys);
            partIds.AddRange(Batteries.Keys);
            partIds.AddRange(Platforms.Keys);
            partIds.AddRange(GroundStations.Keys);
            partIds.AddRange(Solar.Keys);
            foreach (string id in partIds)
                if (!Texts.Contains("part." + id)) warnings.Add($"texts.json: missing 'part.{id}'");

            foreach (var m in Missions.Values)
            {
                if (!Texts.Contains(m.titleKey)) warnings.Add($"texts.json: missing '{m.titleKey}'");
                if (!string.IsNullOrEmpty(m.descriptionKey) && !Texts.Contains(m.descriptionKey))
                    warnings.Add($"texts.json: missing '{m.descriptionKey}'");
            }
            return warnings;
        }

        void CheckGoalTargets(List<Goal> goals, string missionId)
        {
            if (goals == null) return;
            foreach (var g in goals)
            {
                if (string.IsNullOrEmpty(g.target)) continue;
                var exists = TargetCatalog(g.type);
                if (exists == null) continue;   // goal type has no catalog to check against
                if (!exists(g.target))
                    throw new FormatException($"missions.json: '{missionId}' goal '{g.type.ToSnakeCase()}' targets unknown id '{g.target}'");
            }
        }

        // Which catalog a goal's target id must come from. No current goal type uses a target;
        // add a case here when e.g. an instrument goal is introduced (return Instruments.ContainsKey).
        Func<string, bool> TargetCatalog(GoalType type)
        {
            switch (type)
            {
                default: return null;
            }
        }

        static IReadOnlyDictionary<string, T> ById<T>(List<T> list, Func<T, string> getId, string fileName)
        {
            var dict = new Dictionary<string, T>();
            foreach (var item in list)
            {
                string id = getId(item);
                if (string.IsNullOrEmpty(id)) throw new FormatException($"{fileName}: entry without id");
                if (dict.ContainsKey(id)) throw new FormatException($"{fileName}: '{id}' is a duplicate id");
                dict[id] = item;
            }
            return dict;
        }
    }
}
