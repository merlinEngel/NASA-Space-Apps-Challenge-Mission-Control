using NUnit.Framework;
using MissionCore;

namespace MissionCore.Tests
{
    static class TestHelpers
    {
        public const double MuEarth = 3.986004418e14;   // m³/s²
        public const double REarth  = 6378137;          // m
        public const double MuSun   = 1.32712440018e20; // m³/s²
        public const double AU      = 1.495978707e11;   // m
        public const double Deg     = System.Math.PI / 180;

        public static void AssertVec(Vec3d expected, Vec3d actual, double tol) =>
            Assert.That(Vec3d.Distance(expected, actual), Is.LessThan(tol),
                        $"expected {expected}, was {actual}");
    }
}
