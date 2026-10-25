using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Simulation;
namespace MissionCore.Budgets
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

            double missionDurationY = ctx.Lifetime.MissionDurationS / (CoreConstants.DaysPerYear * CoreConstants.SecondsPerDay);
            double avoidanceMPerS = catalog.Balance.Review.AvoidanceDeltaVPerYearMPerS * missionDurationY;
            double dragMakeupMPerS = 0;
            if (ctx.Lifetime.LifetimeS < ctx.Lifetime.MissionDurationS)
                dragMakeupMPerS = OrbitDecay.DragCoefficient * ctx.Lifetime.DragAreaM2 / ctx.Mass.TotalMassKg;
            
            double restYears = (ctx.Lifetime.LifetimeS - ctx.Lifetime.MissionDurationS) / (CoreConstants.DaysPerYear * CoreConstants.SecondsPerDay);
            double disposalMPerS = 0;
            if (restYears > catalog.Scoring.DisposalMaxYears)
                disposalMPerS = Maneuvers.PeriapsisLoweringDeltaV(CoreConstants.RadiusEarth + design.AltitudeM, CoreConstants.RadiusEarth + catalog.Balance.Review.DisposalPerigeeM, CoreConstants.MuEarth);

            return new(
                ispS,
                wetMassKg,
                propellantMassKg,
                dragMakeupMPerS,
                avoidanceMPerS,
                disposalMPerS
            );
        }
    }
}