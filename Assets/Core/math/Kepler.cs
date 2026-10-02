using System;
using MissionCore;

public sealed class KeplerOrbit
{
    // Orbital elements (SI, angles in rad)
    public double A    { get; }   // semi-major axis [m]
    public double Ecc  { get; }   // eccentricity [-], 0 <= Ecc < 1
    public double Inc  { get; }   // inclination i
    public double Raan { get; }   // longitude of the ascending node Ω
    public double ArgPe { get; }  // argument of periapsis ω
    public double M0   { get; }   // mean anomaly at epoch
    public double Epoch { get; }  // time at which M0 applies [s]
    public double Mu   { get; }   // gravitational parameter of the central body [m³/s²]

    public double MeanMotion { get; }                  // n [rad/s]
    public double Period => 2 * Math.PI / MeanMotion;  // T [s]

    // rotation factors orbital plane -> space, computed once in the constructor
    readonly double r11, r12, r21, r22, r31, r32;

    public KeplerOrbit(double a, double ecc, double inc, double raan, double argPe,
                       double m0, double epoch, double mu)
    {
        if (a <= 0) throw new ArgumentOutOfRangeException(nameof(a));
        if (ecc < 0 || ecc >= 1) throw new ArgumentOutOfRangeException(nameof(ecc), "only ellipses (0 <= e < 1)");

        A = a; Ecc = ecc; Inc = inc; Raan = raan; ArgPe = argPe; M0 = m0; Epoch = epoch; Mu = mu;
        MeanMotion = Math.Sqrt(mu / (a * a * a));

        double cO = Math.Cos(raan), sO = Math.Sin(raan);
        double cw = Math.Cos(argPe), sw = Math.Sin(argPe);
        double ci = Math.Cos(inc), si = Math.Sin(inc);

        r11 = cO * cw - sO * sw * ci;   r12 = -(cO * sw + sO * cw * ci);
        r21 = sO * cw + cO * sw * ci;   r22 = cO * cw * ci - sO * sw;
        r31 = sw * si;                  r32 = cw * si;
    }

    // Mean anomaly at time t, wrapped to 0 … 2π
    public double MeanAnomalyAt(double t)
    {
        double m = (M0 + MeanMotion * (t - Epoch)) % (2 * Math.PI);
        return m < 0 ? m + 2 * Math.PI : m;
    }

    // Solve Kepler's equation M = E − e·sin E for E with Newton's method
    public static double SolveKepler(double m, double ecc, double tol = 1e-12, int maxIter = 30)
    {
        if (ecc == 0) return m;                    // circle: E = M
        double e = ecc < 0.8 ? m : Math.PI;        // initial guess
        for (int i = 0; i < maxIter; i++)
        {
            double step = (e - ecc * Math.Sin(e) - m) / (1 - ecc * Math.Cos(e));
            e -= step;
            if (Math.Abs(step) < tol) break;
        }
        return e;
    }

    // Rotate a vector from the orbital plane (x to periapsis, y 90° ahead) into space
    Vec3d ToInertial(double px, double py) =>
        new(r11 * px + r12 * py, r21 * px + r22 * py, r31 * px + r32 * py);

    // Position and velocity together (solve Kepler's equation only once)
    public (Vec3d pos, Vec3d vel) StateAt(double t)
    {
        double eAnom = SolveKepler(MeanAnomalyAt(t), Ecc);
        double cosE = Math.Cos(eAnom), sinE = Math.Sin(eAnom);
        double sq = Math.Sqrt(1 - Ecc * Ecc);
        double r = A * (1 - Ecc * cosE);
        double k = Math.Sqrt(Mu * A) / r;

        Vec3d pos = ToInertial(A * (cosE - Ecc), A * sq * sinE);
        Vec3d vel = ToInertial(-k * sinE, k * sq * cosE);
        return (pos, vel);
    }

    public Vec3d PositionAt(double t) => StateAt(t).pos;
    public Vec3d VelocityAt(double t) => StateAt(t).vel;

    // True anomaly ν (for display)
    public double TrueAnomalyAt(double t)
    {
        double eAnom = SolveKepler(MeanAnomalyAt(t), Ecc);
        return 2 * Math.Atan2(Math.Sqrt(1 + Ecc) * Math.Sin(eAnom / 2),
                              Math.Sqrt(1 - Ecc) * Math.Cos(eAnom / 2));
    }

    // Helper constructor for Earth satellites on a circular orbit
    public static KeplerOrbit Circular(double altitude, double inc, double raan,
                                       double planetRadius, double mu, double epoch = 0) =>
        new(planetRadius + altitude, 0, inc, raan, 0, 0, epoch, mu);
}