using System;
using MissionCore.Catalogs;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;

namespace MissionCore.Budgets
{
    public class PowerBudget : Budget<PowerBudgetResult> 
    {
        public override PowerBudgetResult Evaluate(BudgetContext ctx)
        {
            Catalog catalog = ctx.Catalog;
            MissionDesign design = ctx.Design;

            double r = CoreConstants.RadiusEarth + design.AltitudeM;
            double mu = CoreConstants.MuEarth;
            double orbitPeriodS = 2 * Math.PI * Math.Sqrt(r * r * r / mu);

            MissionTemplate mission = null;
            if (!string.IsNullOrEmpty(design.MissionId))
                catalog.Missions.TryGetValue(design.MissionId, out mission);

            double eclipseFraction = Shadow.EclipseFraction(design.AltitudeM, 0);
            if (mission != null)
                eclipseFraction = Shadow.WorstEclipseFraction(design.AltitudeM, design.InclinationDeg * CoreConstants.DegToRad, mission.raanDeg*CoreConstants.DegToRad, mission.EpochSeconds, mission.durationDays * CoreConstants.SecondsPerDay);

            double sunGenerationW = 0;
            double missionDurationYears = 0;
            if (mission != null)
                missionDurationYears = mission.durationDays/CoreConstants.DaysPerYear;
            if (!string.IsNullOrEmpty(design.SolarCellId) && catalog.SolarCells.TryGetValue(design.SolarCellId, out SolarCellSpec solarCellSpec))
                sunGenerationW =
                    CoreConstants.SolarConstant *
                    design.SolarAreaM2 *
                    solarCellSpec.packingFactor *
                    solarCellSpec.efficiency *
                    Math.Pow(1 - solarCellSpec.degradationPerYear, missionDurationYears);

            double consumptionW = GetConsumptionW(catalog, design);

            double marginW = consumptionW * catalog.Balance.PowerMarginEarlyPhase;

            double batteryUsableWh = 0;
            if (!string.IsNullOrEmpty(design.BatteryId) && catalog.Batteries.TryGetValue(design.BatteryId, out BatterySpec batterySpec))
                batteryUsableWh = batterySpec.energyWh * batterySpec.maxDepthOfDischarge;
            batteryUsableWh *= design.BatteryCount;

            return new(
                orbitPeriodS,
                eclipseFraction,
                sunGenerationW,
                consumptionW,
                marginW,
                batteryUsableWh
            );
        }

        public double GetConsumptionW(Catalog catalog, MissionDesign design)
        {
            double consumption = 0;

            if (!string.IsNullOrEmpty(design.PlatformId) && catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                consumption += platformSpec.busPowerW;
            foreach (string instrumentId in design.InstrumentIds)
            {
                if (!string.IsNullOrEmpty(instrumentId) && catalog.Instruments.TryGetValue(instrumentId, out InstrumentSpec instrumentSpec))
                    consumption += instrumentSpec.powerW;
            }
            if (!string.IsNullOrEmpty(design.CommsId) && catalog.Comms.TryGetValue(design.CommsId, out CommsSpec commsSpec))
                consumption += commsSpec.powerW;

            return consumption;
        }
    }
}