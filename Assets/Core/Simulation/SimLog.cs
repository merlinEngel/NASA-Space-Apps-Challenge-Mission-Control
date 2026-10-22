using System.Collections.Generic;
using System.Linq;

namespace MissionCore.Simulation
{
    public sealed class SimLog
    {
        public IReadOnlyList<SimLogEntry> Entries => entries;
        public IReadOnlyList<SimSample> Samples => samples;
        readonly Dictionary<(LogQuantity, LogCause), SimLogEntry> openEntries = new();
        List<SimLogEntry> entries = new();
        List<SimSample> samples = new();

        // Adds amount for one tick [startS, startS + tickS). Extends the open episode
        // with the same quantity and cause if it ended exactly at startS, else starts a new one.
        public void Add(double startS, double tickS, LogQuantity quantity, LogCause cause, double amount)
        {
            if (amount <= 0) return;

            if (openEntries.TryGetValue((quantity, cause), out SimLogEntry last) && last.EndS == startS)
            {
                last.EndS += tickS;
                last.Amount += amount;
                return;
            }

            SimLogEntry newEntry = new(startS, startS + tickS, quantity, cause, amount);
            entries.Add(newEntry);
            openEntries[(quantity, cause)] = newEntry;
        }

        public void AddSample(SimSample sample)
        {
            samples.Add(sample);
        }

        // Sum of all entries with this quantity and cause.
        public double Total(LogQuantity quantity, LogCause cause) => entries.Where(e => e.Quantity == quantity && e.Cause == cause).Sum(e => e.Amount);
    }
}