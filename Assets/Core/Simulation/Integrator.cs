using System.Collections.Generic;

namespace MissionCore
{
    public static class Integrator
    {
        public static SpacecraftState RK4Step(SpacecraftState state, double t, double dt, ICelestialBody primary, IReadOnlyList<ICelestialBody> otherBodies)
        {
            Vec3d k1r = state.velocity;
            Vec3d k1v = Forces.Acceleration(state.relPos, t, primary, otherBodies);

            Vec3d k2r = state.velocity + dt/2 * k1v;
            Vec3d k2v = Forces.Acceleration(state.relPos + dt/2 * k1r, t + dt/2, primary, otherBodies);

            Vec3d k3r = state.velocity + dt/2 * k2v;
            Vec3d k3v = Forces.Acceleration(state.relPos + dt/2 * k2r, t + dt/2, primary, otherBodies);

            Vec3d k4r = state.velocity + dt * k3v;
            Vec3d k4v = Forces.Acceleration(state.relPos + dt * k3r, t + dt, primary, otherBodies);

            return new(state.relPos + dt/6 * (k1r + 2*k2r + 2*k3r + k4r), state.velocity + dt/6 * (k1v + 2*k2v + 2*k3v + k4v), state.mass, state.dryMass);
        }
    }
}