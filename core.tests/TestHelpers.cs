using System.IO;
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

        // Assets/StreamingAssets/Data, found by walking up from the test assembly (core.tests/bin/...)
        public static string StreamingDataDir()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Data");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            Assert.Fail("Assets/StreamingAssets/Data not found above " + TestContext.CurrentContext.TestDirectory);
            return null;
        }

        // balance_rules.json, scoring.json and sim_rules.json live in the Rules subfolder
        public static string RulesFile(string file) => Path.Combine(StreamingDataDir(), "Rules", file);

        public static BalanceRules LoadRealBalanceRules() =>
            CatalogLoader.LoadObject<BalanceRules>(File.ReadAllText(RulesFile("balance_rules.json")), "balance_rules.json");

        public static void AssertVec(Vec3d expected, Vec3d actual, double tol) =>
            Assert.That(Vec3d.Distance(expected, actual), Is.LessThan(tol),
                        $"expected {expected}, was {actual}");
    }
}
