using static MissionCore.Numerics.CoreConstants;

namespace MissionCore.Earth {public static class EarthRotation
{
    // Greenwich Mean Sidereal Time [rad, 0..2π]: angle from the vernal equinox (x axis) to the Greenwich meridian.
    // IAU 1982 formula, UT1 approximated by UTC (error < 0.004°).
    public static double Gmst(double t)
    {
        double d = t / SecondsPerDay;
        double T = d / 36525;
        double angle = (GmstAtJ2000Deg + d * GmstRateDegPerDay + GmstQuadraticDeg * T * T) % 360;
        return (angle < 0 ? angle + 360 : angle)*DegToRad;
    }
}}