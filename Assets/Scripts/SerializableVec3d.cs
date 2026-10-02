using MissionCore;

namespace MissionGame
{
    [System.Serializable]
    public struct SerializableVec3d
    {
        public double x, y, z;

        public static implicit operator Vec3d(SerializableVec3d v) => new(v.x, v.y, v.z);
    }
}