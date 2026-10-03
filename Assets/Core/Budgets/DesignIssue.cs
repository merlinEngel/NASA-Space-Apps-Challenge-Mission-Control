namespace MissionCore
{
    public enum DesignIssueCode
    {
        //Required Parts
        MissingPlatform,
        MissingMission,
        MissingLauncher,
        MissingComms,
        MissingBattery,
        MissingSolarCell,
        NoGroundStation,

        //Existing Parts
        UnknownGroundStation,
        UnknownPart,

        //Mission Needs
        TooFewInstruments,
        AltitudeOutsideMission,

        //Orbit and Rocket Specs
        OrbitNotSupported,
        InvalidInclination,
        LauncherCannotReachOrbit,
        RideshareOrbitMismatch,
        RideshareAltitudeMismatch,

        //Platform Boundaries
        SolarAreaTooLarge,
        MissionLongerThanLifetime,

        //Communication
        NoCompatibleGroundStation,

        //Valid Numbers
        InvalidValue,
        PropellantWithoutPropulsion
    }

    public class DesignIssue
    {
        public DesignIssue(DesignIssueCode code, string detail = null)
        {
            Code = code;
            Detail = detail;
        }

        public DesignIssueCode Code { get; }
        public string Detail { get; }
    }
}