namespace MissionCore.Tests
{
    // Runs a single budget the same way DesignValidator does:
    // the mass budget first, because cost and delta-v read ctx.Mass.
    internal static class BudgetRunner
    {
        static BudgetContext Context(Catalog catalog, MissionDesign design)
        {
            var ctx = new BudgetContext(catalog, design);
            ctx.Mass = new MassBudget().Evaluate(ctx);
            return ctx;
        }

        public static MassBudgetResult Mass(Catalog catalog, MissionDesign design) => Context(catalog, design).Mass;
        public static CostBudgetResult Cost(Catalog catalog, MissionDesign design) => new CostBudget().Evaluate(Context(catalog, design));
        public static DeltaVBudgetResult DeltaV(Catalog catalog, MissionDesign design) => new DeltaVBudget().Evaluate(Context(catalog, design));
        public static PowerBudgetResult Power(Catalog catalog, MissionDesign design) => new PowerBudget().Evaluate(Context(catalog, design));
        public static DataBudgetResult Data(Catalog catalog, MissionDesign design) => new DataBudget().Evaluate(Context(catalog, design));
    }
}
