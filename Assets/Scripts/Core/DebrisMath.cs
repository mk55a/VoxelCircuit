using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>Pure helpers for voxel debris. No Unity components, so they are unit tested.</summary>
    public static class DebrisMath
    {
        /// <summary>
        /// Unit vector along the cylinder surface at <paramref name="sourceAngle"/>, pointing away from
        /// <paramref name="fromAngle"/> (e.g. away from the player), using the shortest way round the ring.
        /// </summary>
        public static Vector3 AwayAlongSurface(float fromAngle, float sourceAngle)
        {
            float delta = CylinderSpace.ShortestDelta(fromAngle, sourceAngle);
            float side = delta >= 0f ? 1f : -1f;
            return CylinderSpace.Tangent(sourceAngle) * side;
        }

        /// <summary>Take every Nth voxel so that at most <paramref name="maxPieces"/> pieces are emitted.</summary>
        public static int Stride(int voxelCount, int maxPieces)
        {
            if (maxPieces <= 0) return int.MaxValue;
            return Mathf.Max(1, Mathf.CeilToInt(voxelCount / (float)maxPieces));
        }

        /// <summary>Size multiplier that keeps roughly the same total volume when fewer, bigger pieces are used.</summary>
        public static float SizeForStride(int stride) => Mathf.Pow(Mathf.Max(1, stride), 1f / 3f);
    }
}
