using System;
using System.Collections.Generic;
using MissionCore.Numerics;

namespace MissionCore.Simulation
{
    public sealed class SimSummary
    {
        public double DurationDays {get; private set; }
        public EndReason EndReason {get; private set; }
        public double DeliveredBytes {get; private set; }
        public double LostBytes {get; private set; }
        public IReadOnlyDictionary<LogCause, double> LostBytesByCause {get; private set; }
        public double EnergyShortageWh {get; private set; }
        public double FinalAltitudeM {get; private set; }
        public double MinBatterySoc {get; private set; }          // lowest value in the curve samples

        public static SimSummary From(MissionSimulation sim)
        {
            return new SimSummary
            {
                DurationDays = sim.State.MissionTimeS / CoreConstants.SecondsPerDay,
                EndReason = sim.State.EndReason,
                DeliveredBytes = sim.State.DeliveredBits/8,
                LostBytes = sim.State.LostBits/8,
                LostBytesByCause = GetLostBytesByCause(sim),
                EnergyShortageWh = sim.Log.Total(LogQuantity.EnergyWh, LogCause.EnergyShortage),
                FinalAltitudeM = sim.State.AltitudeM,
                MinBatterySoc = GetMinBatterySoC(sim)
            };
        }

        private static double GetMinBatterySoC(MissionSimulation sim)
        {
            double minSoC = sim.State.BatterySoc;
            foreach(SimSample sample in sim.Log.Samples)
                minSoC = Math.Min(minSoC, sample.BatterySoc);
            return minSoC;
        }

        private static IReadOnlyDictionary<LogCause, double> GetLostBytesByCause(MissionSimulation sim)
        {
            Dictionary<LogCause, double> lostBytesByCause = new();

            foreach (LogCause cause in Enum.GetValues(typeof(LogCause)))
            {
                lostBytesByCause.Add(cause, sim.Log.Total(LogQuantity.DataBits, cause)/8);
            }

            return lostBytesByCause;
        }

        public override string ToString()
        {
            string result = $"{DurationDays} days, {DeliveredBytes / 1e9} GB delivered";
            if (LostBytes > 0)
            {
                result += $", {LostBytes / 1e9} GB lost, ";
                foreach ((LogCause cause, double amount) in LostBytesByCause)
                {
                    result += $"({cause}: {amount/1e9} GB)";
                }
            }
            return result;
        }
    }
}