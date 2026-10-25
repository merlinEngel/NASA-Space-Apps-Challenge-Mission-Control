using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Simulation;
using MissionCore.Text;

namespace MissionCore.Tests
{
    // P7 step 3: LaunchRoll. Reference rolls from a python port of System.Random (seeded), checked against dotnet:
    //   seed  0 ^ salt -> 0.542090   (unsalted Random(0) gives 0.726243)
    //   seed  7 ^ salt -> 0.929963   fails at 0.90, succeeds at 0.99
    //   seed 42 ^ salt -> 0.035447
    //   seed 44 ^ salt -> 0.990596   fails at 0.99
    //   seeds 0..999 at 0.90: 104 failures
    public class LaunchRollTests
    {
        static LauncherSpec Launcher(string id) => MissionSimulationStartTests.RealCatalog().Launchers[id];

        [Test]
        public void Seed42_Rideshare_Realistic_Succeeds()
        {
            LaunchOutcome o = LaunchRoll.Roll(Launcher("spacex_transporter"), DisplayMode.Realistic, 42);
            Assert.That(o.Roll, Is.EqualTo(0.035447).Within(1e-6));
            Assert.That(o.SuccessRate, Is.EqualTo(0.99));
            Assert.That(o.Success, Is.True);
        }

        [Test]
        public void Seed7_Electron_Realistic_Fails()
        {
            LaunchOutcome o = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Realistic, 7);
            Assert.That(o.Roll, Is.EqualTo(0.929963).Within(1e-6));
            Assert.That(o.Success, Is.False);
        }

        [Test]
        public void Seed7_Rideshare_Realistic_Succeeds() =>
            Assert.That(LaunchRoll.Roll(Launcher("spacex_transporter"), DisplayMode.Realistic, 7).Success, Is.True);

        [Test]
        public void Seed44_Rideshare_Realistic_Fails()
        {
            LaunchOutcome o = LaunchRoll.Roll(Launcher("spacex_transporter"), DisplayMode.Realistic, 44);
            Assert.That(o.Roll, Is.EqualTo(0.990596).Within(1e-6));
            Assert.That(o.Success, Is.False);
        }

        [Test]
        public void Simple_AlwaysSucceeds_SameRoll()
        {
            LaunchOutcome simple = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Simple, 7);
            LaunchOutcome realistic = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Realistic, 7);
            Assert.That(simple.Success, Is.True);
            Assert.That(simple.SuccessRate, Is.EqualTo(1));
            Assert.That(simple.Roll, Is.EqualTo(realistic.Roll));
        }

        [Test]
        public void SameSeed_SameOutcome()
        {
            LaunchOutcome a = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Realistic, 7);
            LaunchOutcome b = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Realistic, 7);
            Assert.That(b.Roll, Is.EqualTo(a.Roll));
            Assert.That(b.Success, Is.EqualTo(a.Success));
        }

        [Test]
        public void Roll_IsIndependentOfSimulationStream()
        {
            double roll = LaunchRoll.Roll(Launcher("electron"), DisplayMode.Realistic, 0).Roll;
            Assert.That(roll, Is.EqualTo(0.542090).Within(1e-6));
            Assert.That(roll, Is.Not.EqualTo(new Random(0).NextDouble()), "the salt keeps launch and event schedule apart");
        }

        [Test]
        public void Electron_FailsAboutOneInTen()
        {
            LauncherSpec electron = Launcher("electron");
            int failures = Enumerable.Range(0, 1000).Count(s => !LaunchRoll.Roll(electron, DisplayMode.Realistic, s).Success);
            Assert.That(failures, Is.EqualTo(104), "exact count for seeds 0..999");
            Assert.That(failures, Is.InRange(85, 115), "about 10 % at success_rate 0.90");
        }

        [Test]
        public void NullLauncher_Throws() =>
            Assert.Throws<ArgumentNullException>(() => LaunchRoll.Roll(null, DisplayMode.Realistic, 1));
    }
}
