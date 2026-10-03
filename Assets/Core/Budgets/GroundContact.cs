using System;
using System.Collections.Generic;

namespace MissionCore
{
    // Result of a ground contact scan, averaged per day.
    public class ContactStats
    {
        public double ContactsPerDay { get; }       // passes summed over all stations
        public double ContactTimeSPerDay { get; }   // time at least one station is visible
        public double LongestGapS { get; }          // longest time no station is visible

        public ContactStats(double contactsPerDay, double contactTimeSPerDay, double longestGapS)
        {
            ContactsPerDay = contactsPerDay;
            ContactTimeSPerDay = contactTimeSPerDay;
            LongestGapS = longestGapS;
        }
    }

    // Estimates ground station visibility for a circular Earth orbit by sampling a few days.
    // Simplifications: spherical, uniformly rotating Earth, no perturbations, RAAN 0 and
    // station longitude measured from the inertial x axis at t = 0.
    public static class GroundContact
    {
        public const int DefaultDays = 3;
        public const double DefaultStepS = 10;

        public static ContactStats Scan(double altitudeM, double inclinationDeg, IReadOnlyList<GroundStationSpec> stations,
                                        int days = DefaultDays, double stepS = DefaultStepS)
        {
            double durationS = days * Constants.SecondsPerDay;
            if (stations == null || stations.Count == 0 || altitudeM <= 0)
                return new ContactStats(0, 0, durationS);

            double radiusM = Constants.RadiusEarth + altitudeM;
            double meanMotion = Math.Sqrt(Constants.MuEarth / (radiusM * radiusM * radiusM));
            double inclinationRad = inclinationDeg * Math.PI / 180;

            int steps = (int)(durationS / stepS);
            var wasVisible = new bool[stations.Count];
            int passes = 0;
            double visibleS = 0, gapS = 0, longestGapS = 0;

            for (int k = 0; k < steps; k++)
            {
                double t = k * stepS;
                Vec3d satellite = SatellitePosition(radiusM, inclinationRad, meanMotion * t);

                bool anyVisible = false;
                for (int s = 0; s < stations.Count; s++)
                {
                    var station = stations[s];
                    bool visible = ElevationDeg(satellite, station.latDeg, station.lonDeg, t) >= station.minElevationDeg;
                    if (visible && !wasVisible[s]) passes++;
                    wasVisible[s] = visible;
                    anyVisible |= visible;
                }

                if (anyVisible)
                {
                    visibleS += stepS;
                    gapS = 0;
                }
                else
                {
                    gapS += stepS;
                    longestGapS = Math.Max(longestGapS, gapS);
                }
            }

            return new ContactStats(passes / (double)days, visibleS / days, longestGapS);
        }

        // Inertial position on a circular orbit, argumentOfLatitude measured from the ascending node.
        public static Vec3d SatellitePosition(double radiusM, double inclinationRad, double argumentOfLatitudeRad) =>
            new Vec3d(Math.Cos(argumentOfLatitudeRad), Math.Sin(argumentOfLatitudeRad), 0)
                .RotateX(inclinationRad) * radiusM;

        // Elevation of the satellite above the station's horizon at time t.
        public static double ElevationDeg(Vec3d satellite, double latDeg, double lonDeg, double t)
        {
            double lat = latDeg * Math.PI / 180;
            double lon = lonDeg * Math.PI / 180 + Constants.EarthRotationRateRadPerS * t;
            var up = new Vec3d(Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat));
            Vec3d lineOfSight = satellite - up * Constants.RadiusEarth;
            double sinElevation = lineOfSight.Dot(up) / lineOfSight.Length;
            return Math.Asin(Math.Clamp(sinElevation, -1, 1)) * 180 / Math.PI;
        }
    }
}
