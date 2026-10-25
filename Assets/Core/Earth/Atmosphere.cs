using System;
using System.Linq;
using MissionCore.Numerics;

namespace MissionCore.Earth
{
    public readonly struct AtmosphereBand
    {
        public AtmosphereBand(double baseAltitudeM, double baseDensity, double scaleHeightM)
        {
            BaseAltitudeM = baseAltitudeM;
            BaseDensity = baseDensity;
            ScaleHeightM = scaleHeightM;
        }

        public double BaseAltitudeM { get; }   // h0 [m]
        public double BaseDensity { get; }     // rho0 [kg/m³]
        public double ScaleHeightM { get; }    // H [m]
    }

    public static class Atmosphere
    {
        const double ThermosphereBaseM = 100e3;

        // Exponential atmosphere model, Vallado "Fundamentals of Astrodynamics", table 8-4
        // (based on CIRA-72 / U.S. Standard Atmosphere 1976), mean solar activity.
        // Columns: base altitude [m], density at base [kg/m³], scale height [m]
        public static AtmosphereBand[] Bands { get; } =
        {
            new(     0, 1.225    ,   7249),
            new(  25e3, 3.899e-2 ,   6349),
            new(  30e3, 1.774e-2 ,   6682),
            new(  40e3, 3.972e-3 ,   7554),
            new(  50e3, 1.057e-3 ,   8382),
            new(  60e3, 3.206e-4 ,   7714),
            new(  70e3, 8.770e-5 ,   6549),
            new(  80e3, 1.905e-5 ,   5799),
            new(  90e3, 3.396e-6 ,   5382),
            new( 100e3, 5.297e-7 ,   5877),
            new( 110e3, 9.661e-8 ,   7263),
            new( 120e3, 2.438e-8 ,   9473),
            new( 130e3, 8.484e-9 ,  12636),
            new( 140e3, 3.845e-9 ,  16149),
            new( 150e3, 2.070e-9 ,  22523),
            new( 180e3, 5.464e-10,  29740),
            new( 200e3, 2.789e-10,  37105),
            new( 250e3, 7.248e-11,  45546),
            new( 300e3, 2.418e-11,  53628),
            new( 350e3, 9.518e-12,  53298),
            new( 400e3, 3.725e-12,  58515),
            new( 450e3, 1.585e-12,  60828),
            new( 500e3, 6.967e-13,  63822),
            new( 600e3, 1.454e-13,  71835),
            new( 700e3, 3.614e-14,  88667),
            new( 800e3, 1.170e-14, 124640),
            new( 900e3, 5.245e-15, 181050),
            new(1000e3, 3.019e-15, 268000),
        };

        // Air density [kg/m³] at the given altitude. activityFactor scales the thermosphere (>= 100 km).
        public static double Density(double altitudeM, double activityFactor = 1)
        {
            if (altitudeM < 0) altitudeM = 0;

            int i = Bands.Length - 1;
            while (Bands[i].BaseAltitudeM > altitudeM) i--;
            AtmosphereBand band = Bands[i];

            double density = band.BaseDensity * Math.Exp(-(altitudeM - band.BaseAltitudeM) / band.ScaleHeightM);

            return altitudeM >= ThermosphereBaseM ? density * activityFactor : density;
        }

        // Drag acceleration [m/s²] for a satellite moving through the co-rotating atmosphere.
        // dragAreaM2 = mean cross-section A (Cd is applied here), activityFactor as in Atmosphere.Density.
        public static Vec3d DragAcceleration(Vec3d positionM, Vec3d velocityMps, double massKg, double dragAreaM2, double activityFactor = 1)
        {
            if (dragAreaM2 <= 0 || massKg <= 0) return Vec3d.Zero;

            double h = positionM.Length - CoreConstants.RadiusEarth;

            Vec3d airVelocity = new(-CoreConstants.OmegaEarth * positionM.y, CoreConstants.OmegaEarth * positionM.x, 0);

            Vec3d relVelocity = velocityMps - airVelocity;

            return -0.5 * Density(h, activityFactor) * (OrbitDecay.DragCoefficient * dragAreaM2/massKg) * relVelocity.Length * relVelocity;
        }
    }
}