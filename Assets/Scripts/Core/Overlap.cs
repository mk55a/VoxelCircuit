using UnityEngine;

namespace CylinderShooter.Core
{
    public static class Overlap
    {
        public static bool Spheres(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB)
        {
            float r = radiusA + radiusB;
            return (centerA - centerB).sqrMagnitude <= r * r;
        }
    }
}
