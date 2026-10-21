using System;
using MissionCore.Numerics;

namespace MissionCore.Time
{
    public class SimTimeConverter
    {
        public static double ToSeconds(DateTime utc) => ((utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc) - Constants.J2000DateTime).TotalSeconds;
        public static DateTime ToUtc(double seconds) => Constants.J2000DateTime.AddSeconds(seconds);
        public static double JulianDate(double seconds) => Constants.JulianDateJ2000 + seconds / Constants.SecondsPerDay;
        public static double JulianCenturies(double seconds) => (JulianDate(seconds) - Constants.JulianDateJ2000) / 36525;
    }
}