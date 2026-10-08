using System;

namespace MissionCore
{
    public class PowerBudget : Budget<PowerBudgetResult> 
    {
        public override PowerBudgetResult Evaluate(BudgetContext ctx)
        {
            Catalog catalog = ctx.Catalog;
            MissionDesign design = ctx.Design;

            double r = Constants.RadiusEarth + design.AltitudeM;
            double mu = Constants.MuEarth;
            double orbitPeriodS = 2 * Math.PI * Math.Sqrt(r * r * r / mu);

            double eclipseFraction = Math.Asin(Constants.RadiusEarth / r) / Math.PI;

            double sunGenerationW = 0;
            double missionDurationYears = 0;
            if (!string.IsNullOrEmpty(design.MissionId) && catalog.Missions.TryGetValue(design.MissionId, out MissionTemplate missionTemplate))
                missionDurationYears = missionTemplate.durationDays/Constants.DaysPerYear;
            if (!string.IsNullOrEmpty(design.SolarCellId) && catalog.SolarCells.TryGetValue(design.SolarCellId, out SolarCellSpec solarCellSpec))
                sunGenerationW =
                    Constants.SolarConstant *
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