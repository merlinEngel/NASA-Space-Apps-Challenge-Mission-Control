namespace MissionCore
{
    public class MassBudgetResult
    {
        public double DryMassKg { get; }
        public double PropellantMassKg { get; }
        public double MarginKg { get; }
        public double LimitKg { get; }
        public double TotalMassKg { get => DryMassKg + PropellantMassKg + MarginKg; }
        public double ReserveKg { get => LimitKg - TotalMassKg; }
        public double ReserveFraction { get => ReserveKg/LimitKg; }

        public MassBudgetResult(double dryMassKg, double propellantMassKg, double marginKg, double limitKg)
        {
            DryMassKg = dryMassKg;
            PropellantMassKg = propellantMassKg;
            MarginKg = marginKg;
            LimitKg = limitKg;
        }
    }
}