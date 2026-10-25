using MissionCore.Bodies;
using MissionCore.Numerics;
using System;
using static MissionCore.Numerics.CoreConstants;
using static System.Math;

namespace MissionCore.Earth
{
    public static class J2Drift
    {
        // Secular drift caused by Earth's oblateness (J2), in rad/s.
        public static double NodalRate(double semiMajorAxisM, double eccentricity, double inclinationRad)
        {
            if (semiMajorAxisM <= 0 || eccentricity >= 1 || eccentricity < 0) throw new ArgumentOutOfRangeException();
            
            double k = K(semiMajorAxisM, eccentricity);

            return -1.5 * k * Cos(inclinationRad);
        }

        public static double ApsidalRate(double semiMajorAxisM, double eccentricity, double inclinationRad)
        {
            if (semiMajorAxisM <= 0 || eccentricity >= 1 || eccentricity < 0) throw new ArgumentOutOfRangeException();

            double k = K(semiMajorAxisM, eccentricity);

            return 0.75 * k * (5 * Pow(Cos(inclinationRad), 2) - 1);
        }

        private static double K(double semiMajorAxisM, double eccentricity)
        {
            double mu = MuEarth;
            
            double n = Sqrt(mu/Pow(semiMajorAxisM, 3));
            double p = semiMajorAxisM * (1 - (eccentricity*eccentricity));

            return n * J2Earth * Pow(RadiusEarth/p, 2);
        }

        public static double MeanAnomalyRate(double semiMajorAxisM, double eccentricity, double inclinationRad)
        {
            return 0.75 * K(semiMajorAxisM, eccentricity) * Sqrt(1-eccentricity*eccentricity)*(3*Pow(Cos(inclinationRad), 2) - 1);
        }

        public static double? SunSynchronousInclinationDeg(double altitudeM)
        {
            double a = RadiusEarth + altitudeM;
            double n = Sqrt(MuEarth/Pow(a, 3));
            double nodalRateSun = 2*PI / (TropicalYearDays * SecondsPerDay);

            double cosI = -nodalRateSun / (1.5 * n * J2Earth * Pow(RadiusEarth/a, 2));

            return cosI < -1 ? null : Acos(cosI) * 180 / PI;
        }

        // Returns a copy of the orbit that drifts under Earth's J2.
        public static KeplerOrbit ApplyTo(KeplerOrbit orbit)
        {
            if (Abs(orbit.Mu - MuEarth) > 1e6) throw new ArgumentException("J2Drift only applies to Earth orbits", nameof(orbit));

            double mar = MeanAnomalyRate(orbit.A, orbit.Ecc, orbit.Inc);
            double nr = NodalRate(orbit.A, orbit.Ecc, orbit.Inc);
            double ar = ApsidalRate(orbit.A, orbit.Ecc, orbit.Inc);

            return new( orbit.A, orbit.Ecc, orbit.Inc, orbit.RaanRad, orbit.ArgPe, orbit.M0, orbit.Epoch, orbit.Mu,
                        raanRate: nr, argPeRate: ar, meanAnomalyRate: mar);
        }

        // Extra acceleration from Earth's oblateness [m/s²]; position in the Earth-centred equatorial frame.
        public static Vec3d J2Acceleration(Vec3d positionM)
        {
            double r = positionM.Length;
            double k = 1.5 * J2Earth * MuEarth * Pow(RadiusEarth, 2) / Pow(r, 5);
            double f = 5 * Pow(positionM.z, 2) / Pow(r, 2);
            return new(k * positionM.x * (f - 1), k * positionM.y * (f - 1), k * positionM.z * (f - 3));
        }
    }
}