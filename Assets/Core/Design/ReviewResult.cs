using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionCore.Design
{
    public sealed class ReviewResult
    {
        public ReviewResult(DesignReport report, IReadOnlyList<ReviewItem> items)
        {
            Report = report;
            Items = items;
        }

        public DesignReport Report { get; }
        public IReadOnlyList<ReviewItem> Items { get; }

        public bool CanLaunch => !Items.Any(item => item.Status == Rules.BudgetStatus.Red);
        public IEnumerable<ReviewItem> Risks => Items.Where(item => item.Status == Rules.BudgetStatus.Yellow);   // all Yellow items, the player confirms these
        public ReviewItem Item(ReviewCode code) => Items.First(item => item.Code == code);   // the item with that code (exactly one per code)

        // True if nothing is red and every yellow item is in confirmed.
        public bool CanLaunchWith(IEnumerable<ReviewCode> confirmed)
        {
            if (confirmed == null) throw new ArgumentNullException(nameof(confirmed));
            HashSet<ReviewCode> confirmedSet = new(confirmed);
            return CanLaunch && Risks.All(r => confirmedSet.Contains(r.Code));
        }
}
}