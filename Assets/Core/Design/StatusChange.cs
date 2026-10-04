namespace MissionCore
{
    public readonly struct StatusChange
    {
        public StatusChange(BudgetKind kind, BudgetStatus from, BudgetStatus to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public BudgetKind Kind { get; }
        public BudgetStatus From { get; }
        public BudgetStatus To { get; }
        public bool IsWorse => To > From;
    }
}