namespace MissionCore.Design
{
    // The order here is the order of the checklist.
    public enum ReviewCode
    {
        // Red: block the launch
        LaunchMassTooHigh,
        BudgetExceeded,
        RequiredInstrumentMissing,
        NoGroundStation,

        // Yellow: risks the player has to confirm
        LowPowerReserve,
        StorageFillsUp,
        NoAvoidancePropellant,
        NoDisposalPropellant,
        LifetimeShorterThanMission
    }
}
