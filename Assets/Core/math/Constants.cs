using System;

namespace MissionCore.Numerics
{
    public static class CoreConstants
    {
        public static readonly DateTime J2000DateTime = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public const double G0Earth = 9.80665;
        public const double RadiusEarth = 6378137;
        public const double MuEarth = 3.986004418e+14;
        public const double SolarConstant = 1361;
        public const double SecondsPerDay = 86400;
        public const double DaysPerYear = 365.25;

        public const double GmstAtJ2000Deg = 280.46061837;         // Greenwich angle at 2000-01-01 12:00 UTC
        public const double GmstRateDegPerDay = 360.98564736629;   // one turn plus ~1° per day (sidereal rotation)
        public const double GmstQuadraticDeg = 0.000387933;        // precession term, multiplied by T² (centuries)

        public const double J2Earth = 1.08262668e-3;
        public const double AstronomicalUnit = 1.495978707e11;
        public const double ObliquityJ2000Deg = 23.439281;
        public const double JulianDateJ2000 = 2451545.0;

        public const double TropicalYearDays = 365.2421897;

        public const double DegToRad = Math.PI / 180;
        public const double RadToDeg = 180 / Math.PI;

        public const double OmegaEarth = 7.2921159e-5;
    }
}