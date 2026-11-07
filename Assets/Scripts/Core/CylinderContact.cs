using UnityEngine;

namespace CylinderShooter.Core
{
    public static class CylinderContact
    {
        private const float MaxSegmentAngle = Mathf.PI / 32f;

        public static bool SweptSpheres(CylinderPosition startA, CylinderPosition endA, float radiusA,
            CylinderPosition startB, CylinderPosition endB, float radiusB, float cylinderRadius)
        {
            float deltaA = CylinderSpace.ShortestDelta(startA.Angle, endA.Angle);
            float deltaB = CylinderSpace.ShortestDelta(startB.Angle, endB.Angle);
            int segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(deltaA), Mathf.Abs(deltaB)) / MaxSegmentAngle));
            Vector3 previousA = startA.ToWorld(cylinderRadius);
            Vector3 previousB = startB.ToWorld(cylinderRadius);
            float combinedRadius = radiusA + radiusB;
            // Account for the maximum arc-to-chord error in each segment.
            float tolerance = cylinderRadius * (2f - Mathf.Cos(deltaA / (2f * segments))
                - Mathf.Cos(deltaB / (2f * segments)));
            float limit = combinedRadius + tolerance;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 nextA = CylinderPosition.Lerp(startA, endA, t).ToWorld(cylinderRadius);
                Vector3 nextB = CylinderPosition.Lerp(startB, endB, t).ToWorld(cylinderRadius);
                Vector3 relativeStart = previousA - previousB;
                Vector3 relativeMovement = (nextA - previousA) - (nextB - previousB);
                float lengthSquared = relativeMovement.sqrMagnitude;
                float closest = lengthSquared > 0f
                    ? Mathf.Clamp01(-Vector3.Dot(relativeStart, relativeMovement) / lengthSquared)
                    : 0f;
                if ((relativeStart + relativeMovement * closest).sqrMagnitude <= limit * limit)
                {
                    return true;
                }
                previousA = nextA;
                previousB = nextB;
            }
            return false;
        }
    }
}
