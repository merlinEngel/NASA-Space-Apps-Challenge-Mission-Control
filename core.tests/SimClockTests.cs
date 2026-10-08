using System;
using NUnit.Framework;

namespace MissionCore.Tests
{
    public class SimClockTests
    {
        // Executes the due steps, like PlanetManager.Update does
        static int Run(SimClock c, double dt)
        {
            int n = c.Advance(dt);
            for (int i = 0; i < n; i++) c.Tick();
            return n;
        }

        [Test]
        public void SameStepCount_RegardlessOfFrameSplit()
        {
            var a = new SimClock(60) { TimeScale = 1000 };
            var b = new SimClock(60) { TimeScale = 1000 };
            var rng = new Random(42);
            int stepsA = 0;
            double total = 0;
            for (int i = 0; i < 1000; i++)
            {
                double dt = rng.NextDouble() * 0.05;
                total += dt;
                stepsA += Run(a, dt);
            }
            int stepsB = Run(b, total);
            Assert.That(stepsA, Is.EqualTo(stepsB).Within(1));   // at most rounding at the edge
        }

        [Test]
        public void TimeScaleOneToOne()
        {
            var c = new SimClock(60);           // TimeScale 1
            Assert.That(Run(c, 59.9), Is.EqualTo(0));
            Assert.That(Run(c, 0.2), Is.EqualTo(1));
            Assert.That(c.MissionTime, Is.EqualTo(60));
        }

        [Test]
        public void TimeScale3600_OneOrbitInOnePointFiveSeconds()
        {
            // "Done when" from P1: 92.6 min at 3600× is about 1.54 s of real time
            var c = new SimClock(60) { TimeScale = 3600 };
            for (int i = 0; i < 93; i++) Run(c, 1.0 / 60);   // 93 frames at 60 FPS
            Assert.That(c.MissionTime / 60, Is.EqualTo(93).Within(1));   // minutes
        }

        [Test]
        public void Pause_ReturnsNoSteps()
        {
            var c = new SimClock(60) { TimeScale = 1000, Paused = true };
            Assert.That(c.Advance(10), Is.EqualTo(0));
            Assert.That(c.RenderTime, Is.EqualTo(0));
        }

        [Test]
        public void Times_AreConsistent()
        {
            var c = new SimClock(60, epochSeconds: 6609600) { TimeScale = 100 };
            Run(c, 1.5);                                   // 150 s: 2 steps + 30 s remainder
            Assert.That(c.StepIndex, Is.EqualTo(2));
            Assert.That(c.MissionTime, Is.EqualTo(120));
            Assert.That(c.SimTime, Is.EqualTo(6609600 + 120));
            Assert.That(c.RenderTime, Is.EqualTo(6609600 + 150).Within(1e-9));
        }

        [Test]
        public void RenderTime_AlwaysWithinCurrentStep()
        {
            var c = new SimClock(60) { TimeScale = 777 };
            var rng = new Random(1);
            for (int i = 0; i < 500; i++)
            {
                Run(c, rng.NextDouble() * 0.1);
                Assert.That(c.RenderTime, Is.GreaterThanOrEqualTo(c.SimTime)
                                            .And.LessThan(c.SimTime + c.StepSeconds));
            }
        }

        [Test]
        public void TooManySteps_AreCapped_AndReported()
        {
            var c = new SimClock(60) { MaxStepsPerAdvance = 10 };
            Assert.That(Run(c, 60 * 100), Is.EqualTo(10));
            Assert.That(c.IsLagging, Is.True);
            Assert.That(c.RenderTime, Is.EqualTo(c.SimTime));   // backlog dropped

            Run(c, 30);                                          // back to normal afterwards
            Assert.That(c.IsLagging, Is.False);
        }

        [Test]
        public void InvalidStep_IsRejected() =>
            Assert.That(() => new SimClock(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}