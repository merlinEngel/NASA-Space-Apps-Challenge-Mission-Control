using MissionCore.Mission;

namespace MissionCore.Simulation
{
    // An event of the mission with its drawn start time.
    public readonly struct ScheduledEvent
    {
        public readonly EventType Type;
        public readonly double TimeS;   // mission time, a whole number of ticks

        public ScheduledEvent(EventType type, double timeS)
        {
            Type = type;
            TimeS = timeS;
        }
    }
}