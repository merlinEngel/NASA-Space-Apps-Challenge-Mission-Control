using MissionCore.Design;
using MissionCore.Earth;

namespace MissionCore.Budgets
{
    public class LifetimeBudgetResult : BudgetResult
    {
        public LifetimeBudgetResult(double lifetimeS, double missionDurationS)
        {
            LifetimeS = lifetimeS;
            MissionDurationS = missionDurationS;
        }

        public double LifetimeS { get; }          // orbital lifetime from OrbitDecay
        public double MissionDurationS { get; }   // from the mission template
        public bool IsCapped => LifetimeS == OrbitDecay.MaxLifetimeS;         // true if LifetimeS == OrbitDecay.MaxLifetimeS
        public override double ReserveFraction => MissionDurationS > 0 ? (LifetimeS - MissionDurationS) / MissionDurationS : 1;
        public override Metric TotalMetric => Metric.LifetimeS;
        public override double Total => LifetimeS;
    }
}