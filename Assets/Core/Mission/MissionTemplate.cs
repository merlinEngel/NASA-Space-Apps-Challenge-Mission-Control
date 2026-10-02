using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MissionCore
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
    }
}