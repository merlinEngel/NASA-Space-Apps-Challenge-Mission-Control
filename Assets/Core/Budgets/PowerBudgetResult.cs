namespace MissionCore
{
    public class PowerBudgetResult
    {
        public PowerBudgetResult(double orbitPeriodS, double eclipseFraction, double sunGenerationW, double consumptionW, double marginW, double batteryUsableWh)
        {
            OrbitPeriodS = orbitPeriodS;
            EclipseFraction = eclipseFraction;
            SunGenerationW = sunGenerationW;
            ConsumptionW = consumptionW;
            MarginW = marginW;
            BatteryUsableWh = batteryUsableWh;
        }

        public double OrbitPeriodS { get; }
        public double EclipseFraction { get; }
        public double SunGenerationW { get; }
        public double ConsumptionW { get; }
        public double MarginW { get; }
        public double BatteryUsableWh { get; }

        public double RequiredW { get => ConsumptionW + MarginW; }
        public double AverageGenerationW { get => SunGenerationW * (1-EclipseFraction); }
        public double EclipseEnergyNeedWh { get => RequiredW * EclipseFraction * OrbitPeriodS / 3600; }
        public double ReserveW { get => AverageGenerationW - RequiredW; }
        public double ReserveFraction { get => RequiredW > 0 ? ReserveW / RequiredW : 0; }
        public bool BatteryOk { get => EclipseEnergyNeedWh <= BatteryUsableWh; }
    }
}