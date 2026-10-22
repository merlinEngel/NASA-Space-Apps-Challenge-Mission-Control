using System.IO;
using NUnit.Framework;

namespace MissionCore.Tests
{
    // P6 step 8: the simrun console tool, called through SimRun.Run without starting a process
    public class SimRunTests
    {
        static (int code, string text) Run(params string[] args)
        {
            var output = new StringWriter();
            // --data explicitly: the test runner's working folder is inside core.tests/bin
            string[] full = new string[args.Length + 2];
            args.CopyTo(full, 0);
            full[args.Length] = "--data";
            full[args.Length + 1] = TestHelpers.StreamingDataDir();
            int code = SimRun.Run(full, output);
            return (code, output.ToString());
        }

        [Test]
        public void TechDemoGood_Seed42_PrintsSummary()
        {
            (int code, string text) = Run("tech_demo_good", "--seed", "42");
            TestContext.Out.WriteLine(text);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(text, Does.Contain("90 days, 38.86 GB delivered, 0.00 GB lost"));
            Assert.That(text, Does.Contain("DurationReached"));
            Assert.That(text, Does.Contain("SolarStorm on day 33.36"));
        }

        [Test]
        public void SameArgumentsTwice_IdenticalOutput()
        {
            Assert.That(Run("tech_demo_good", "--seed", "42").text, Is.EqualTo(Run("tech_demo_good", "--seed", "42").text));
        }

        [Test]
        public void WithoutSeed_SameAsSeed42()
        {
            Assert.That(Run("tech_demo_good").text, Is.EqualTo(Run("tech_demo_good", "--seed", "42").text));
        }

        [Test]
        public void OtherSeed_OtherStormDay()
        {
            Assert.That(Run("tech_demo_good", "--seed", "7").text, Does.Contain("SolarStorm on day 27.66"));
        }

        [Test]
        public void LowPower_NamesEnergyShortage()
        {
            (int code, string text) = Run("tech_demo_low_power");

            Assert.That(code, Is.EqualTo(0));
            Assert.That(text, Does.Contain("(EnergyShortage 18.23 GB)"));
            Assert.That(text, Does.Not.Contain("StorageFull"), "causes without loss are left out");
        }

        [Test]
        public void UnknownDesign_ExitCode1WithName()
        {
            (int code, string text) = Run("does_not_exist");

            Assert.That(code, Is.EqualTo(1));
            Assert.That(text, Does.Contain("does_not_exist"));
        }

        [Test]
        public void BadSeed_ExitCode1()
        {
            Assert.That(Run("tech_demo_good", "--seed", "abc").code, Is.EqualTo(1));
        }

        [Test]
        public void NoDesign_ExitCode1WithUsage()
        {
            (int code, string text) = Run();

            Assert.That(code, Is.EqualTo(1));
            Assert.That(text, Does.Contain("usage"));
        }

        [Test]
        public void TimeOption_IsNotTakenAsDesign()
        {
            (int code, string text) = Run("tech_demo_good", "--time");

            Assert.That(code, Is.EqualTo(0));
            Assert.That(text, Does.Contain("ms"));
        }
    }
}
