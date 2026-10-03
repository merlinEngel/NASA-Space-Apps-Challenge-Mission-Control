using System;

namespace MissionCore
{
    public static class Maneuvers
    {

        public static (Vec3d prograde, Vec3d normal, Vec3d radial) LocalFrame(Vec3d relPos, Vec3d velocity)
        {
            Vec3d prograde = velocity.Normalized;
            Vec3d normal = relPos.Cross(velocity).Normalized;
            Vec3d radial = prograde.Cross(normal);

            return (prograde, normal, radial);
        }

        // Hohmann transfer between two circular orbits with radii r1 → r2 [m].
        // dv1/dv2 are prograde Δv [m/s] (negative = retrograde when lowering the orbit),
        // transferTime [s] is half the transfer ellipse: burn 2 happens this long after burn 1.
        public static (double dv1, double dv2, double transferTime) HohmannDeltaVs(double r1, double r2, double mu)
        {
            double a_t = (r1 + r2)/2;

            double v1 = Math.Sqrt(mu/r1);
            double v_p = Math.Sqrt(mu*(2/r1 - 1/a_t));
            double dv1 = v_p - v1;

            double v2 = Math.Sqrt(mu/r2);
            double v_a = Math.Sqrt(mu*(2/r2 - 1/a_t));
            double dv2 = v2 - v_a;

            double transferTime = Math.PI * Math.Sqrt(a_t * a_t * a_t / mu);

            return (dv1, dv2, transferTime);
        }

        // Δv [m/s] the remaining fuel can still provide (rocket equation)
        public static double AvailableDeltaV(SpacecraftState state, double isp) =>
            isp * Constants.G0Earth * Math.Log(state.mass / state.dryMass);

        public static bool TryApplyImpulse(SpacecraftState state, double dvPrograde, double dvNormal, double dvRadial, double isp, out SpacecraftState newState)
        {
            if (isp <= 0) throw new("isp must be greater than 0");

            newState = state;

            (Vec3d prograde, Vec3d normal, Vec3d radial) = LocalFrame(state.relPos, state.velocity);

            Vec3d dvVec = dvPrograde*prograde + dvNormal*normal + dvRadial*radial;

            double newMass = state.mass * Math.Exp(-dvVec.Length / (isp * Constants.G0Earth));
            if (newMass < state.dryMass)
                return false;

            Vec3d newVelocity = state.velocity + dvVec;

            newState = new(state.relPos, newVelocity, newMass, state.dryMass);
            return true;
        }
    }
}