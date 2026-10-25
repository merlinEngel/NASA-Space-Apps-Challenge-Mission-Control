using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using MissionCore.Design;
using MissionCore.Numerics;
using MissionCore.Text;
using MissionCore.Time;

namespace MissionCore.Mission
{
    public class MissionTemplate
    {
        public string id;
        public string titleKey, descriptionKey;
        [JsonProperty("budget_USD")] public double budgetUSD;
        public double durationDays;
        public ValueRange altitudeM;
        public int minInstruments;
        public List<Goal> requiredGoals, bonusGoals;
        public SolarActivity solarActivity;
        public DateTime epochUtc;
        public double raanDeg;
        public List<FixedEvent> fixedEvents;
        public string source;

        public double DurationYears => durationDays / CoreConstants.DaysPerYear;
        public double EpochSeconds => SimTimeConverter.ToSeconds(epochUtc);

        public Dictionary<string, string> TextValues(DisplayFormatter f) => new()
        {
            ["altitude_min"] = f.Value(Metric.AltitudeM, altitudeM.Min),
            ["altitude_max"] = f.Value(Metric.AltitudeM, altitudeM.Max),
            ["budget"] = f.Value(Metric.CostUsd, budgetUSD),
            ["duration_days"] = durationDays.ToString("0", f.Culture),
            ["min_instruments"] = minInstruments.ToString(f.Culture),
        };
    }
}