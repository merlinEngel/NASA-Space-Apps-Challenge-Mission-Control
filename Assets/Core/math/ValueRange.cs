namespace MissionCore
{
    public class ValueRange
    {
        public ValueRange(double min, double max)
        {
            Min = min;
            Max = max;
        }

        public double Min {get; set;}
        public double Max {get; set;}

        public bool Contains(double value) => Min <= value && value <= Max;
    }
}