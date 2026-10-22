namespace MissionCore.Simulation
{
    // One episode: consecutive ticks with the same quantity and cause.
    public sealed class SimLogEntry
    {
        public SimLogEntry(double startS, double endS, LogQuantity quantity, LogCause cause, double amount)
        {
            StartS = startS;
            EndS = endS;
            Quantity = quantity;
            Cause = cause;
            Amount = amount;
        }

        public double StartS { get; }        // mission time when the episode began
        public double EndS { get; set; }     // mission time after its last tick
        public LogQuantity Quantity { get; }
        public LogCause Cause { get; }
        public double Amount { get; set; }   // bits or Wh, summed over the episode
    }
}