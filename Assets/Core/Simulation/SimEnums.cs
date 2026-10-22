namespace MissionCore.Simulation
{
    public enum LogQuantity { DataBits, EnergyWh }

    // Why something was lost or not produced
    public enum LogCause { EnergyShortage, StorageFull, SafeMode }

    public enum EndReason { None, DurationReached, Reentry, BatteryDepleted }
}