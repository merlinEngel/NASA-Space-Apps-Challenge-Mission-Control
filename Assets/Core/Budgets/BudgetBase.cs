namespace MissionCore
{
    public abstract class Budget<TResult> where TResult : BudgetResult
    {
        public abstract TResult Evaluate(BudgetContext context);
    }

    public abstract class BudgetResult
    {
        public abstract Metric TotalMetric { get; }
        public abstract double ReserveFraction { get; }
        public abstract double Total { get; }
    }

    public class BudgetContext
    {
        public BudgetContext(Catalog catalog, MissionDesign design)
        {
            Catalog = catalog;
            Design = design;
        }

        public Catalog Catalog { get; }
        public MissionDesign Design { get; }
        public MassBudgetResult Mass { get; set; }   // filled after the mass budget ran
                                                     // ... later results if another budget needs them
    }
}