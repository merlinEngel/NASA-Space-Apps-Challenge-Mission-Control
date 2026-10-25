using System.Collections.Generic;
using MissionCore.Rules;

internal static class Constants
{
    public static IReadOnlyDictionary<BudgetStatus, string> StatusColorKeys = new Dictionary<BudgetStatus, string>
    {
        [BudgetStatus.Green] = "Green",
        [BudgetStatus.Yellow] = "Yellow",
        [BudgetStatus.Red] = "Red"
    };
}