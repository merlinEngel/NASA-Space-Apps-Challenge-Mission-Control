namespace MissionCore
{
    public readonly struct SpacecraftState
    {
        public readonly Vec3d relPos;
        public readonly Vec3d velocity;
        public readonly double mass;
        public readonly double dryMass;

        public SpacecraftState(Vec3d relPos, Vec3d velocity, double mass, double dryMass)
        {
            this.dryMass = dryMass;
            this.relPos = relPos;
            this.velocity = velocity;
            this.mass = mass;
        }
    }
}