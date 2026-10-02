namespace MissionCore
{
    // Values from scoring.json: how a finished mission is turned into points and stars.
    public class ScoringRules
    {
        public ScoreWeights Weights { get; set; }

        // Delivering more than this multiple of the required data gives no extra points.
        public double DataCapRatio { get; set; }

        public double SafetyPenaltyPerYellowRisk { get; set; }
        public double SafetyPenaltyDisposalRule { get; set; }

        // Satellite must burn up within this many years after the mission ends (FCC rule).
        public double DisposalMaxYears { get; set; }

        public StarRules Stars { get; set; }
        public string Source { get; set; }
    }

    // Share of each sub-score in the total (all together = 1.0)
    public class ScoreWeights
    {
        public double Data { get; set; }
        public double Budget { get; set; }
        public double Safety { get; set; }
    }

    public class StarRules
    {
        public int RequiredGoals { get; set; }
        public int PerBonusGoal { get; set; }
        public int Max { get; set; }
    }
}
