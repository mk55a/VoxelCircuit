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
        
        public static bool SegmentSphere(Vector3 start, Vector3 end, float radiusA, Vector3 center, float radiusB)
        {
            Vector3 segment = end - start;
            float lengthSq = segment.sqrMagnitude;
            float t = lengthSq > 0f ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / lengthSq) : 0f;
            float r = radiusA + radiusB;
            return (start + segment * t - center).sqrMagnitude <= r * r;
        }
    }
}
