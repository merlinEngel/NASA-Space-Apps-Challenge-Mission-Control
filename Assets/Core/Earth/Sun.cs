using MissionCore.Numerics;
using static System.Math;

namespace MissionCore.Earth{public static class Sun
{
    // Unit vector from Earth to the Sun in the Earth-centred equatorial frame (J2000).
    // Low-precision formula from the Astronomical Almanac, accuracy about 0.01°.
    const double Deg = PI / 180;

    // Unit vector from Earth to the Sun in the Earth-centred equatorial frame (J2000).
    // Low-precision formula from the Astronomical Almanac, accuracy about 0.01°.
    public static Vec3d Direction(double t)
    {
        double n = t / Constants.SecondsPerDay;                 // days since J2000

        double L = 280.460 + 0.9856474 * n;                       // mean longitude [deg]
        double g = (357.528 + 0.9856003 * n) * Deg;               // mean anomaly [rad]
        double lambda = (L + 1.915 * Sin(g) + 0.020 * Sin(2 * g)) * Deg;   // ecliptic longitude [rad]
        double e = Constants.ObliquityJ2000Deg * Deg;

        return new Vec3d(Cos(lambda), Cos(e) * Sin(lambda), Sin(e) * Sin(lambda));
    }

    // Earth-Sun distance [AU]
    public static double DistanceAu(double t)
    {
        double n = t / Constants.SecondsPerDay;
        double g = (357.528 + 0.9856003 * n) * Deg;

        return 1.00014 - 0.01671 * Cos(g) - 0.00014 * Cos(2 * g);
    }
}}