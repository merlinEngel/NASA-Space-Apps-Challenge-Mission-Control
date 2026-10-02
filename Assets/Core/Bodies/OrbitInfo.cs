using System;

namespace MissionCore
{
    public readonly struct OrbitInfo
    {
        public readonly double apoapsis;
        public readonly double periapsis;
        public readonly double orbitalPeriod;
        public readonly double inclination;

        private OrbitInfo(double apoapsis, double periapsis, double orbitalPeriod, double inclination)
        {
            this.apoapsis = apoapsis;
            this.periapsis = periapsis;
            this.orbitalPeriod = orbitalPeriod;
            this.inclination = inclination;
        }

        // Below this eccentricity the orbit counts as circular (at 800 km altitude: apo and peri < ~1.5 km apart).
        // Moon and Sun slightly deform a circular orbit, so the threshold is not 0.
        public const double CircularEccentricity = 1e-4;

        // Time [s] until the next apoapsis. Circular orbit: 0 (every point is equally high → now). Escape trajectory: NaN.
        public static double TimeToApoapsis(Vec3d relPos, Vec3d velocity, double mu)
        {
            double e = Eccentricity(relPos, velocity, mu);
            if (e >= 1) return double.NaN;
            if (e < CircularEccentricity) return 0;

            if (!TryMeanAnomaly(relPos, velocity, mu, out double M, out double n))
                return double.NaN;

            double t = (Math.PI - M) / n;              // apoapsis is at M = π
            if (t < 0) t += 2 * Math.PI / n;           // already passed → next orbit
            return t;
        }

        // Time [s] until the next periapsis. Circular orbit: 0 (now). Escape trajectory: NaN.
        public static double TimeToPeriapsis(Vec3d relPos, Vec3d velocity, double mu)
        {
            double e = Eccentricity(relPos, velocity, mu);
            if (e >= 1) return double.NaN;
            if (e < CircularEccentricity) return 0;

            if (!TryMeanAnomaly(relPos, velocity, mu, out double M, out double n))
                return double.NaN;

            return (2 * Math.PI - M) / n;              // periapsis is at M = 0 or 2π
        }

        // Eccentricity vector: points from the planet to the periapsis, its length is the eccentricity e
        public static Vec3d EccentricityVector(Vec3d relPos, Vec3d velocity, double mu) =>
            ((velocity.SqrLength - mu / relPos.Length) * relPos - relPos.Dot(velocity) * velocity) / mu;

        public static double Eccentricity(Vec3d relPos, Vec3d velocity, double mu) =>
            EccentricityVector(relPos, velocity, mu).Length;

        static bool TryMeanAnomaly(Vec3d relPos, Vec3d velocity, double mu, out double M, out double n)
        {
            M = n = 0;
            double r = relPos.Length;
            double rv = relPos.Dot(velocity);          // rVec · vVec

            // 1. Eccentricity vector (points to the periapsis), length = e
            Vec3d eVec = EccentricityVector(relPos, velocity, mu);
            double e = eVec.Length;
            if (e < CircularEccentricity || e >= 1) return false;

            // 2. True anomaly ν: angle periapsis → satellite
            double nu = Math.Acos(Math.Clamp(eVec.Dot(relPos) / (e * r), -1, 1));
            if (rv < 0) nu = 2 * Math.PI - nu;         // approaching → second half of the orbit

            // 3. Eccentric anomaly E
            double E = 2 * Math.Atan2(Math.Sqrt(1 - e) * Math.Sin(nu / 2), Math.Sqrt(1 + e) * Math.Cos(nu / 2));
            if (E < 0) E += 2 * Math.PI;

            // 4. Mean anomaly M (Kepler's equation) – grows uniformly with time
            M = E - e * Math.Sin(E);

            // 5. Mean motion n = √(μ/a³), a from the energy
            double a = -mu / (2 * (velocity.SqrLength / 2 - mu / r));
            n = Math.Sqrt(mu / (a * a * a));
            return true;
        }

        public static OrbitInfo FromState(Vec3d relPos, Vec3d velocity, double mu, Vec3d axis)
        {
            double speed = velocity.Length;
            double energy = speed * speed / 2 - mu / relPos.Length;
            double a = -mu / (2 * energy);

            Vec3d h = relPos.Cross(velocity);
            // Max(0, …): on exact circular orbits rounding can give a tiny negative value → otherwise NaN
            double eccentricity = Math.Sqrt(Math.Max(0, 1 + 2 * energy * Math.Pow(h.Length, 2) / Math.Pow(mu, 2)));

            double periapsis = a * (1 - eccentricity);
            double apoapsis = a * (1 + eccentricity);

            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(a, 3) / mu);
            if (a <= 0)
                orbitalPeriod = -1;

            // Normalize the axis; Clamp because rounding can give slightly more than 1 → acos would be NaN
            double inclination = Math.Acos(Math.Clamp(h.Dot(axis.Normalized) / h.Length, -1, 1));

            return new(apoapsis, periapsis, orbitalPeriod, inclination);
        }
    }
}