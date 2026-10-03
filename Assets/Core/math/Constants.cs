using System;

namespace MissionCore
{
    public static class Constants
    {
        public static readonly DateTime J2000DateTime = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public const double G0Earth = 9.80665;
        public const double RadiusEarth = 6378137;
        public const double MuEarth = 3.986004418e+14;
        public const double SolarConstant = 1361;
        public const double SecondsPerDay = 86400;
        public const double DaysPerYear = 365.25;
        public const double EarthRotationRateRadPerS = 7.2921159e-5;
    }
}