using System;
using MissionCore.Catalogs;
using MissionCore.Text;

namespace MissionCore.Simulation
{
    public readonly struct LaunchOutcome
    {
        public LaunchOutcome(bool success, double roll, double successRate)
        {
            Success = success;
            Roll = roll;
            SuccessRate = successRate;
        }
        public bool Success { get; }
        public double Roll { get; }         // the random number in [0, 1), also drawn in Simple mode
        public double SuccessRate { get; }  // 1 in Simple mode, launcher.successRate in Realistic mode
    }

    public static class LaunchRoll
    {
        public const int LaunchSeedSalt = 0x4C41554E;   // "LAUN" in ASCII, keeps the launch roll apart from the event schedule

        public static LaunchOutcome Roll(LauncherSpec launcher, DisplayMode mode, int seed)
        {
            if (launcher == null) throw new ArgumentNullException();
            double roll = new Random(seed ^ LaunchSeedSalt).NextDouble();
            double rate = mode == DisplayMode.Simple ? 1 : launcher.successRate;
            return new LaunchOutcome(roll < rate, roll, rate);
        }
    }
}