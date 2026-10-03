using System;

namespace MissionCore
{
    public class DeltaVBudgetResult
    {
        public DeltaVBudgetResult(double ispS, double wetMassKg, double propellantMassKg, double requiredDeltaVMPerS)
        {
            IspS = ispS;
            WetMassKg = wetMassKg;
            PropellantMassKg = propellantMassKg;
            RequiredDeltaVMPerS = requiredDeltaVMPerS;
        }

        public double IspS { get; }
        public double WetMassKg { get; }
        public double PropellantMassKg { get; }
        public double RequiredDeltaVMPerS { get; }

        public double EmptyMassKg { get => WetMassKg - PropellantMassKg; }
        public double AvailableDeltaVMPerS { get => IspS > 0 && EmptyMassKg > 0 ? IspS * Constants.G0Earth * Math.Log(WetMassKg/EmptyMassKg) : 0; }
        public double ReserveMPerS { get => AvailableDeltaVMPerS - RequiredDeltaVMPerS; }
        public double ReserveFraction { get => RequiredDeltaVMPerS > 0 ? ReserveMPerS/RequiredDeltaVMPerS : 0; }
    }
}