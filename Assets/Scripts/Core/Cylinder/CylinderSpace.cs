using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Pure cylinder math. Angles are radians. World point:
    /// x = r*sin(angle), z = r*cos(angle), y = height.
    /// </summary>
    public static class CylinderSpace
    {
        public const float TwoPi = Mathf.PI * 2f;

        /// <summary>Wraps an angle into [0, 2π).</summary>
        public static float WrapAngle(float angle)
        {
            float wrapped = angle % TwoPi;
            if (wrapped < 0f) wrapped += TwoPi;
            if (wrapped >= TwoPi) wrapped = 0f;
            return wrapped;
        }

        /// <summary>Signed shortest rotation from <paramref name="from"/> to <paramref name="to"/>, in (-π, π].</summary>
        public static float ShortestDelta(float from, float to)
        {
            float delta = WrapAngle(to - from);
            if (delta > Mathf.PI) delta -= TwoPi;
            return delta;
        }

        public static Vector3 ToWorld(float angle, float height, float radius)
        {
            return new Vector3(radius * Mathf.Sin(angle), height, radius * Mathf.Cos(angle));
        }

        /// <summary>Unit vector pointing away from the cylinder axis at this angle.</summary>
        public static Vector3 Outward(float angle)
        {
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        /// <summary>Unit vector along the surface in the direction of increasing angle.</summary>
        public static Vector3 Tangent(float angle)
        {
            return new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));
        }

        public static float ArcToAngle(float arcLength, float radius) => arcLength / radius;
    }
}
