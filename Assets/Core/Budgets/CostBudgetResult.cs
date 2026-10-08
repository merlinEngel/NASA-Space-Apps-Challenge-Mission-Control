namespace MissionCore
{
    public class CostBudgetResult : BudgetResult
    {
        public CostBudgetResult(double hardwareCostUSD, double launchCostUSD, double operationsCostUSD, double budgetUSD)
        {
            HardwareCostUSD = hardwareCostUSD;
            LaunchCostUSD = launchCostUSD;
            OperationsCostUSD = operationsCostUSD;
            BudgetUSD = budgetUSD;
        }

        public double HardwareCostUSD { get; }
        public double LaunchCostUSD { get; }
        public double OperationsCostUSD { get; } //TODO
        public double BudgetUSD { get; }

        public double TotalCostUSD => HardwareCostUSD + LaunchCostUSD + OperationsCostUSD;
        public double ReserveUSD => BudgetUSD - TotalCostUSD;
        public override double ReserveFraction => BudgetUSD > 0 ? ReserveUSD / BudgetUSD : 0;

        public override Metric TotalMetric => Metric.CostUsd;
        public override double Total => TotalCostUSD;
    }
}