using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MissionCore
{
    public class DesignReport
    {
        public DesignReport(MassBudgetResult massResult, CostBudgetResult costResult, DeltaVBudgetResult deltaVResult, PowerBudgetResult powerResult, DataBudgetResult dataResult, IReadOnlyDictionary<BudgetKind, BudgetStatus> statuses, IReadOnlyList<DesignIssue> issues)
        {
            MassResult = massResult;
            CostResult = costResult;
            DeltaVResult = deltaVResult;
            PowerResult = powerResult;
            DataResult = dataResult;
            Statuses = statuses;
            Issues = issues;
        }

        public MassBudgetResult MassResult { get; }
        public CostBudgetResult CostResult { get; }
        public DeltaVBudgetResult DeltaVResult { get; }
        public PowerBudgetResult PowerResult { get; }
        public DataBudgetResult DataResult { get; }

        public IReadOnlyDictionary<BudgetKind, BudgetStatus> Statuses { get; }
        public IReadOnlyList<DesignIssue> Issues { get; }

        public bool IsValid => Issues.Count == 0;
        public bool AnyRed => Statuses.Values.Contains(BudgetStatus.Red);
        public bool AllGreen => IsValid && Statuses.Values.All(s => s == BudgetStatus.Green);



        public BudgetStatus Status(Catalog catalog, BudgetKind k) => k switch
        {
            BudgetKind.Cost => catalog.Balance.StatusFor(BudgetKind.Cost, CostResult.ReserveFraction),
            BudgetKind.Data => catalog.Balance.StatusFor(BudgetKind.Data, DataResult.ReserveFraction),
            BudgetKind.DeltaV => catalog.Balance.StatusFor(BudgetKind.DeltaV, DeltaVResult.ReserveFraction),
            BudgetKind.Mass => catalog.Balance.StatusFor(BudgetKind.Mass, MassResult.ReserveFraction),
            BudgetKind.Power => catalog.Balance.StatusFor(BudgetKind.Power, PowerResult.ReserveFraction),
            _ => throw new System.NotImplementedException()
        };

        public double Value(Metric m) => m switch
        {
            Metric.PowerUseW => PowerResult.ConsumptionW,
            Metric.PowerGenerationW => PowerResult.AverageGenerationW,
            Metric.MassKg => MassResult.TotalMassKg,
            Metric.DeltaVMPerS => DeltaVResult.AvailableDeltaVMPerS,
            Metric.DataGeneratedBitsPerDay => DataResult.GeneratedBitsPerDay,
            Metric.DataDownlinkBitsPerDay => DataResult.DownlinkBitsPerDay,
            Metric.CostUsd => CostResult.TotalCostUSD,
            _ => throw new System.NotImplementedException()
        };

        public string Format(bool richText)
        {
            var sb = new StringBuilder();

            string verdict = !IsValid ? "INVALID" : AnyRed ? "NOT FLYABLE" : AllGreen ? "ALL GREEN" : "FLYABLE (tight)";
            sb.AppendLine($"=== Design report: {verdict} ===");

            AppendBudget(sb, richText, BudgetKind.Mass, "Mass",
                $"{Num(MassResult.TotalMassKg)} / {Num(MassResult.LimitKg)} kg", MassResult.ReserveFraction);
            AppendBudget(sb, richText, BudgetKind.Cost, "Cost",
                $"{Num(CostResult.TotalCostUSD / 1e6, "0.00")} / {Num(CostResult.BudgetUSD / 1e6, "0.00")} M USD", CostResult.ReserveFraction);
            AppendBudget(sb, richText, BudgetKind.DeltaV, "Delta-v",
                $"{Num(DeltaVResult.AvailableDeltaVMPerS)} available / {Num(DeltaVResult.RequiredDeltaVMPerS)} needed m/s", DeltaVResult.ReserveFraction);
            AppendBudget(sb, richText, BudgetKind.Power, "Power",
                $"{Num(PowerResult.AverageGenerationW)} avg / {Num(PowerResult.RequiredW)} needed W, battery {(PowerResult.BatteryOk ? "ok" : "too small")}", PowerResult.ReserveFraction);
            AppendBudget(sb, richText, BudgetKind.Data, "Data",
                $"{Num(DataResult.DownlinkBitsPerDay / 1e9, "0.00")} down / {Num(DataResult.GeneratedBitsPerDay / 1e9, "0.00")} generated Gbit/day, storage {(DataResult.StorageOk ? "ok" : "too small")}", DataResult.ReserveFraction);

            sb.AppendLine(Issues.Count == 0 ? "Issues: none" : $"Issues ({Issues.Count}):");
            foreach (DesignIssue issue in Issues)
                sb.AppendLine(issue.Detail == null ? $"  - {issue.Code}" : $"  - {issue.Code} ({issue.Detail})");

            return sb.ToString().TrimEnd();
        }

        private void AppendBudget(StringBuilder sb, bool richText, BudgetKind kind, string label, string values, double reserveFraction)
        {
            string status = Statuses.TryGetValue(kind, out BudgetStatus s) ? s.ToString().ToUpperInvariant() : "?";
            if (richText) status = $"<color={StatusColor(s)}>{status}</color>";
            sb.AppendLine($"{label,-8} {status,-6} {values} (reserve {Num(reserveFraction * 100, "+0;-0")} %)");
        }

        private static string StatusColor(BudgetStatus status) => status switch
        {
            BudgetStatus.Green => "#4CAF50",
            BudgetStatus.Yellow => "#FFC107",
            _ => "#F44336",
        };

        // Invariant: always "." as decimal separator. Infinity/NaN as text instead of "∞"/"NaN".
        private static string Num(double value, string format = "0.#")
        {
            if (double.IsNaN(value)) return "n/a";
            if (double.IsInfinity(value)) return value > 0 ? "inf" : "-inf";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}