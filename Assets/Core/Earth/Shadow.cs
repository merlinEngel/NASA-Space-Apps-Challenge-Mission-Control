using MissionCore.Numerics;
using static System.Math;

namespace MissionCore.Earth{public static class Shadow
{
    public static bool IsInShadow(Vec3d positionM, Vec3d sunDirection)
    {
        bool onNightside = positionM.Dot(sunDirection) < 0;
        bool behindEarth = positionM.RejectFrom(sunDirection).Length < Constants.RadiusEarth;

        return onNightside && behindEarth;
    }

    // Angle between the Sun direction and the orbit plane [rad], positive if the Sun is on the north side.
    public static double BetaAngle(double inclinationRad, double raanRad, Vec3d sunDirection)
    {
        Vec3d n = new(Sin(inclinationRad) * Sin(raanRad), -Sin(inclinationRad) * Cos(raanRad), Cos(inclinationRad));
        return Asin(n.Dot(sunDirection));
    }

    // Fraction of one circular orbit spent in Earth's shadow (cylinder model), 0..0.5
    public static double EclipseFraction(double altitudeM, double betaRad)
    {
        double r = Constants.RadiusEarth + altitudeM;
        double criticalBetaRad = Asin(Constants.RadiusEarth / r);

        if (Abs(betaRad) >= criticalBetaRad) return 0;
        return Acos(Sqrt(Pow(altitudeM, 2) + 2 * Constants.RadiusEarth * altitudeM) / (r * Cos(betaRad))) / PI;
    }

    // Largest eclipse fraction of a J2-drifting circular orbit over the mission, sampled once per day.
    public static double WorstEclipseFraction(double altitudeM, double inclinationRad, double raanRad,
                                              double epochS, double durationS)
    {
        double a = Constants.RadiusEarth + altitudeM;
        double raanRate = J2Drift.NodalRate(a, 0, inclinationRad);   // circular orbit: e = 0

        // At least start and end, even for missions shorter than one day
        int days = Max(1, (int)Ceiling(durationS / Constants.SecondsPerDay));

        double worst = 0;
        for (int d = 0; d <= days; d++)
        {
            double dt = Min(d * Constants.SecondsPerDay, durationS);
            double t = epochS + dt;
            double raan = raanRad + raanRate * dt;

            double beta = BetaAngle(inclinationRad, raan, Sun.Direction(t));
            worst = Max(worst, EclipseFraction(altitudeM, beta));
        }
        return worst;
    }
}}