namespace MissionCore
{
    public class CostBudgetResult
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
        
        public double TotalCostUSD { get => HardwareCostUSD + LaunchCostUSD + OperationsCostUSD; }
        public double ReserveUSD { get => BudgetUSD - TotalCostUSD; }
        public double ReserveFraction { get => BudgetUSD > 0 ? ReserveUSD/BudgetUSD : 0; }
    }
}