namespace MissionCore
{
    public class DeltaVBudget : Budget<DeltaVBudgetResult>
    {
        public override DeltaVBudgetResult Evaluate(BudgetContext ctx)
        {
            Catalog catalog = ctx.Catalog;
            MissionDesign design = ctx.Design;
            MassBudgetResult mass = ctx.Mass;

            double ispS = 0;
            if (!string.IsNullOrEmpty(design.PropulsionId) && catalog.Propulsion.TryGetValue(design.PropulsionId, out PropulsionSpec propulsionSpec))
                ispS = propulsionSpec.ispS;

            double wetMassKg = mass.TotalMassKg;
            double propellantMassKg = mass.PropellantMassKg;
            double requiredDeltaVMPerS = 0;

            return new(
                ispS,
                wetMassKg,
                propellantMassKg,
                requiredDeltaVMPerS
            );
        }
    }
}