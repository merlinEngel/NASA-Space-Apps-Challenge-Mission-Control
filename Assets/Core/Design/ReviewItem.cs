using MissionCore.Rules;

namespace MissionCore.Design
{
    public sealed class ReviewItem
    {
        public ReviewItem(ReviewCode code, BudgetStatus status, double value = double.NaN, double limit = double.NaN, string detail = null)
        {
            Code = code;
            Status = status;
            Value = value;
            Limit = limit;
            Detail = detail;
        }

        public ReviewCode Code { get; }
        public BudgetStatus Status { get; }
        public double Value { get; }    // what the design has (kg, USD, count, ...), NaN if not a number
        public double Limit { get; }    // what is allowed or needed, NaN if not a number
        public string Detail { get; }   // extra info that is not a number, e.g. issue codes or a band
    }
}