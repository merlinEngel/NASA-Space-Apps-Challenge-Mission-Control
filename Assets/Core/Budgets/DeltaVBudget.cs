using System.Security.Cryptography.X509Certificates;

namespace MissionCore
{
    public static class DeltaVBudget
    {
        public static DeltaVBudgetResult Evaluate(Catalog catalog, MissionDesign design)
        {
            double ispS = 0;
            if (!string.IsNullOrEmpty(design.PropulsionId) && catalog.Propulsion.TryGetValue(design.PropulsionId, out PropulsionSpec propulsionSpec))
                ispS = propulsionSpec.ispS;

            MassBudgetResult mass = MassBudget.Evaluate(catalog, design);
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