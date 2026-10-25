using System;
using static MissionCore.Numerics.CoreConstants;

namespace MissionCore.Earth
{
    public static class OrbitDecay
    {
        public const double ReentryAltitudeM = 150e3;       // below this the orbit decays within hours
        public const double DragCoefficient = 2.2;          // typical value for satellites
        public const double MaxLifetimeS = 100 * DaysPerYear * SecondsPerDay;   // stop counting after 100 years

        // Change of the semi-major axis per second for a circular orbit [m/s], always <= 0.
        public static double DecayRate(double altitudeM, double ballisticCoefficient, double activityFactor = 1)
        {
            double a = RadiusEarth + altitudeM;
            double rho = Atmosphere.Density(altitudeM, activityFactor);

            return -rho * Math.Sqrt(MuEarth * a) * ballisticCoefficient;
        }

        // Time until the orbit has decayed from altitudeM to ReentryAltitudeM [s], capped at MaxLifetimeS.
        public static double LifetimeS(double altitudeM, double massKg, double dragAreaM2, double activityFactor = 1)
        {
            if (massKg <= 0 || dragAreaM2 < 0) throw new ArgumentOutOfRangeException();
            if (altitudeM < ReentryAltitudeM) return 0;

            double b = DragCoefficient * dragAreaM2 / massKg;
            if (b == 0) return MaxLifetimeS;

            const double StepM = 1000;
            double h = altitudeM;
            double lifetime = 0;
            while (h > ReentryAltitudeM)
            {
                double step = Math.Min(StepM, h - ReentryAltitudeM);
                lifetime += step / Math.Abs(DecayRate(h - step / 2, b, activityFactor));
                h -= step;
                if (lifetime >= MaxLifetimeS) return MaxLifetimeS;
            }

            return lifetime > MaxLifetimeS ? MaxLifetimeS : lifetime;
        }
    }
}