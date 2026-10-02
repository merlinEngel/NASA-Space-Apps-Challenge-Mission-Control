using System;

namespace MissionCore
{
    public sealed class SimClock
    {
        public double StepSeconds { get; }        // fixed step, e.g. 60
        public int MaxStepsPerAdvance { get; set; } = 2000;
        public double TimeScale { get; set; } = 1; // sim seconds per real-time second
        public bool Paused { get; set; }

        public double EpochSeconds { get; }        // start time in seconds since J2000
        public long StepIndex { get; private set; } // number of executed steps
        public bool IsLagging { get; private set; }

        double accumulator;

        public SimClock(double stepSeconds = 60, double epochSeconds = 0)
        {
            if (stepSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            StepSeconds = stepSeconds;
            EpochSeconds = epochSeconds;
        }

        // Simulated time after the last full step, since mission start
        public double MissionTime => StepIndex * StepSeconds;

        // Same in seconds since J2000 (for Kepler and the Sun's position)
        public double SimTime => EpochSeconds + MissionTime;

        // Time for rendering, including the partial step (smooth motion)
        public double RenderTime => SimTime + accumulator;

        // Add real time, return the number of steps that are now due.
        // The caller then executes exactly that many steps and calls Tick() after each one.
        public int Advance(double realDeltaSeconds)
        {
            if (Paused) return 0;

            accumulator += realDeltaSeconds * TimeScale;
            int due = (int)Math.Min(Math.Floor(accumulator / StepSeconds), MaxStepsPerAdvance);
            accumulator -= due * StepSeconds;

            IsLagging = accumulator >= StepSeconds;
            if (IsLagging) accumulator = 0;    // drop the backlog instead of freezing

            return due;
        }

        // Mark one step as done
        public void Tick() => StepIndex++;
    }
}