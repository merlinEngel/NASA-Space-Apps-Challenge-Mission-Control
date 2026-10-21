using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;

namespace MissionCore.Budgets
{
    public class LifetimeBudget : Budget<LifetimeBudgetResult>
    {
        // A flat panel tumbling in all directions shows on average half of one side (Cauchy)
        const double SolarPanelDragFactor = 0.5;

        public override LifetimeBudgetResult Evaluate(BudgetContext context)
        {
            MissionDesign design = context.Design;
            Catalog catalog = context.Catalog;

            PlatformSpec platform = null;
            if (!string.IsNullOrEmpty(design.PlatformId))
                catalog.Platforms.TryGetValue(design.PlatformId, out platform);

            MissionTemplate mission = null;
            if (!string.IsNullOrEmpty(design.MissionId))
                catalog.Missions.TryGetValue(design.MissionId, out mission);

            // Without the mass margin: a heavier satellite lives longer, so the margin would be optimistic
            double massKg = context.Mass.DryMassKg + context.Mass.PropellantMassKg;
            double solarActivityFactor = mission == null ? 1 : catalog.Sim?.DensityFactor(mission.solarActivity) ?? 1;

            double lifetimeS = OrbitDecay.MaxLifetimeS;
            if (platform != null && massKg > 0)
            {
                double dragAreaM2 = platform.dragAreaM2 + design.SolarAreaM2 * SolarPanelDragFactor;
                lifetimeS = OrbitDecay.LifetimeS(design.AltitudeM, massKg, dragAreaM2, solarActivityFactor);
            }

            double missionDurationS = mission == null ? 0 : mission.durationDays * Constants.SecondsPerDay;

            return new(lifetimeS, missionDurationS);
        }
    }
}