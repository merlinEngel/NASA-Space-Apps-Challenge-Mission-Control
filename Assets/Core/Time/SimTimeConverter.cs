using System;
using MissionCore.Numerics;

namespace MissionCore.Time
{
    public class SimTimeConverter
    {
        public static double ToSeconds(DateTime utc) => ((utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc) - CoreConstants.J2000DateTime).TotalSeconds;
        public static DateTime ToUtc(double seconds) => CoreConstants.J2000DateTime.AddSeconds(seconds);
        public static double JulianDate(double seconds) => CoreConstants.JulianDateJ2000 + seconds / CoreConstants.SecondsPerDay;
        public static double JulianCenturies(double seconds) => (JulianDate(seconds) - CoreConstants.JulianDateJ2000) / 36525;
    }
}