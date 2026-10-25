using System;
using MissionCore.Design;
using MissionCore.Numerics;

namespace MissionCore.Budgets
{
    public class DeltaVBudgetResult : BudgetResult
    {
        public DeltaVBudgetResult(double ispS, double wetMassKg, double propellantMassKg, double dragMakeupMPerS, double avoidanceMPerS, double disposalMPerS)
        {
            IspS = ispS;
            WetMassKg = wetMassKg;
            PropellantMassKg = propellantMassKg;
            DragMakeupMPerS = dragMakeupMPerS;
            AvoidanceMPerS = avoidanceMPerS;
            DisposalMPerS = disposalMPerS;
        }

        public double IspS { get; }
        public double WetMassKg { get; }
        public double PropellantMassKg { get; }
        public double RequiredDeltaVMPerS { get; }

        public double EmptyMassKg { get => WetMassKg - PropellantMassKg; }
        // public double AvailableDeltaVMPerS { get => IspS > 0 && EmptyMassKg > 0 ? IspS * CoreConstants.G0Earth * Math.Log(WetMassKg/EmptyMassKg) : 0; }
        public double AvailableDeltaVMPerS => DragMakeupMPerS + AvoidanceMPerS + DisposalMPerS;
        public double ReserveMPerS { get => AvailableDeltaVMPerS - RequiredDeltaVMPerS; }
        public override double ReserveFraction { get => RequiredDeltaVMPerS > 0 ? ReserveMPerS/RequiredDeltaVMPerS : 0; }

        public double DragMakeupMPerS { get; }
        public double AvoidanceMPerS { get; }
        public double DisposalMPerS { get; }

        public double AfterDragMPerS => AvailableDeltaVMPerS - DragMakeupMPerS;
        public double AfterAvoidanceMPerS => AfterDragMPerS - AvoidanceMPerS;

        public override Metric TotalMetric => Metric.DeltaVMPerS;
        public override double Total => AvailableDeltaVMPerS;
    }
}