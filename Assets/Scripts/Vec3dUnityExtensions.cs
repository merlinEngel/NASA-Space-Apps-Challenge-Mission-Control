using UnityEngine;
using MissionCore;

public static class Vec3dUnityExtensions
{
    // Core (Z up, right-handed) → Unity (Y up, left-handed): swap Y and Z
    public static Vector3 ToUnity(this Vec3d p, double scale) =>
        new((float)(p.x * scale), (float)(p.z * scale), (float)(p.y * scale));

    // Way back, e.g. for mouse clicks in the orbit view
    public static Vec3d ToCore(this Vector3 v, double scale) =>
        new(v.x / scale, v.z / scale, v.y / scale);
}