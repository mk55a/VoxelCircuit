using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>Pure helpers for destruction pulses. No Unity components, so they are unit tested.</summary>
    public static class PulseMath
    {
        /// <summary>
        /// Radius of the sphere around the origin that contains every corner of <paramref name="localBounds"/>
        /// after <paramref name="boundsToOrigin"/> is applied (e.g. a renderer's mesh bounds, moved into the
        /// space of the object it belongs to). Used to size an effect to the model automatically.
        /// </summary>
        public static float BoundingRadius(Bounds localBounds, Matrix4x4 boundsToOrigin)
        {
            float maxSq = 0f;
            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z));
                maxSq = Mathf.Max(maxSq, boundsToOrigin.MultiplyPoint3x4(corner).sqrMagnitude);
            }
            return Mathf.Sqrt(maxSq);
        }

        /// <summary>
        /// Unit direction lying flat in the screen plane at <paramref name="surfaceAngle"/> on the cylinder
        /// (spanned by the surface tangent and world up), rotated by <paramref name="radians"/>.
        /// Effects launched this way never fly straight at or away from the side-on camera.
        /// </summary>
        public static Vector3 ScreenPlaneDirection(float surfaceAngle, float radians)
        {
            return CylinderSpace.Tangent(surfaceAngle) * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians);
        }
    }
}
