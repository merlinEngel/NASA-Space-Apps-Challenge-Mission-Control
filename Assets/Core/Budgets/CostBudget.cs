namespace MissionCore
{
    public static class CostBudget
    {
        public static CostBudgetResult Evaluate(Catalog catalog, MissionDesign design)
        {
            double hardwareCost = GetHardwareCost(catalog, design);
            double launchCost = 0;
            if (!string.IsNullOrEmpty(design.LauncherId) && catalog.Launchers.TryGetValue(design.LauncherId, out LauncherSpec launcher))
            {
                double totalMassKg = MassBudget.Evaluate(catalog, design).TotalMassKg;
                launchCost = launcher.GetLaunchCost(totalMassKg);
            }
            double operationsCost = 0;
            double budget = 0;
            if (!string.IsNullOrEmpty(design.MissionId)
                && catalog.Missions.TryGetValue(design.MissionId, out MissionTemplate mission))
            {
                budget = mission.budgetUSD;
            }

            return new (
                hardwareCost,
                launchCost,
                operationsCost,
                budget
            );
        }

        public static double GetHardwareCost(Catalog catalog, MissionDesign design)
        {
            double hardwareCost = 0;
            if (!string.IsNullOrEmpty(design.PlatformId) && catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                hardwareCost += platformSpec.priceUSD;
            foreach (string instrumentId in design.InstrumentIds)
            {
                if (!string.IsNullOrEmpty(instrumentId) && catalog.Instruments.TryGetValue(instrumentId, out InstrumentSpec instrumentSpec))
                    hardwareCost += instrumentSpec.priceUSD;
            }
            if (!string.IsNullOrEmpty(design.PropulsionId) && catalog.Propulsion.TryGetValue(design.PropulsionId, out PropulsionSpec propulsionSpec))
                hardwareCost += propulsionSpec.priceUsd;
            if (!string.IsNullOrEmpty(design.CommsId) && catalog.Comms.TryGetValue(design.CommsId, out CommsSpec commsSpec))
                hardwareCost += commsSpec.priceUSD;
            if (!string.IsNullOrEmpty(design.BatteryId) && catalog.Batteries.TryGetValue(design.BatteryId, out BatterySpec batterySpec))
                hardwareCost += batterySpec.priceUSD * design.BatteryCount;
            if (!string.IsNullOrEmpty(design.SolarCellId) && catalog.SolarCells.TryGetValue(design.SolarCellId, out SolarCellSpec solarCellSpec))
                hardwareCost += solarCellSpec.pricePerAreaUSDM2 * design.SolarAreaM2;

            return hardwareCost;
        }
    }
}