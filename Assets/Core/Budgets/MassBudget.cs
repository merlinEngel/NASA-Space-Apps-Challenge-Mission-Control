using System;

namespace MissionCore
{
    public class MassBudget : Budget<MassBudgetResult>
    {
        public override MassBudgetResult Evaluate(BudgetContext ctx)
        {
            Catalog catalog = ctx.Catalog;
            MissionDesign design = ctx.Design;

            double dryMassKg = GetDryMass(catalog, design);
            double marginKg = dryMassKg * catalog.Balance.MassMarginEarlyPhase;
            double propellantMassKg = design.PropellantMassKg;
            double limitKg = double.PositiveInfinity;
            if (!string.IsNullOrEmpty(design.LauncherId) && catalog.Launchers.TryGetValue(design.LauncherId, out LauncherSpec launcherSpec))
            {
                if (launcherSpec.IsRideshare)
                    limitKg = launcherSpec.maxPayloadPerCustomerKg;
                else
                {
                    switch (OrbitClassifier.Classify(design.AltitudeM, design.InclinationDeg))
                    {
                        case LaunchOrbit.Leo:
                            limitKg = launcherSpec.leoKg;
                            break;
                        case LaunchOrbit.Sso:
                            limitKg = launcherSpec.ssoKg ?? launcherSpec.leoKg;
                            break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(design.PlatformId) && catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                limitKg = Math.Min(limitKg, platformSpec.maxMassKg);

            return new MassBudgetResult(
                dryMassKg,
                propellantMassKg,
                marginKg,
                limitKg
            );
        }

        public double GetDryMass(Catalog catalog, MissionDesign design)
        {
            double dryMass = 0;
            if (!string.IsNullOrEmpty(design.PlatformId) && catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                dryMass += platformSpec.busMassKg;
            foreach (string instrumentId in design.InstrumentIds)
            {
                if (!string.IsNullOrEmpty(instrumentId) && catalog.Instruments.TryGetValue(instrumentId, out InstrumentSpec instrumentSpec))
                    dryMass += instrumentSpec.massKg;
            }
            if (!string.IsNullOrEmpty(design.PropulsionId) && catalog.Propulsion.TryGetValue(design.PropulsionId, out PropulsionSpec propulsionSpec))
                dryMass += propulsionSpec.dryMassKg;
            if (!string.IsNullOrEmpty(design.CommsId) && catalog.Comms.TryGetValue(design.CommsId, out CommsSpec commsSpec))
                dryMass += commsSpec.massKg;
            if (!string.IsNullOrEmpty(design.BatteryId) && catalog.Batteries.TryGetValue(design.BatteryId, out BatterySpec batterySpec))
                dryMass += batterySpec.massKg * design.BatteryCount;
            if (!string.IsNullOrEmpty(design.SolarCellId) && catalog.SolarCells.TryGetValue(design.SolarCellId, out SolarCellSpec solarCellSpec))
                dryMass += solarCellSpec.massPerAreaKgM2 * design.SolarAreaM2;

            return dryMass;
        }
    }
}