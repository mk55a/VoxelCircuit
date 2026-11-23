using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>A location on the cylinder: angle (radians, always wrapped) and height.</summary>
    public readonly struct CylinderPosition
    {
        public readonly float Angle;
        public readonly float Height;

        public CylinderPosition(float angle, float height)
        {
            Angle = CylinderSpace.WrapAngle(angle);
            Height = height;
        }

        public CylinderPosition Moved(float deltaAngle, float deltaHeight) =>
            new CylinderPosition(Angle + deltaAngle, Height + deltaHeight);

        public CylinderPosition WithHeight(float height) => new CylinderPosition(Angle, height);

        public static CylinderPosition Lerp(CylinderPosition a, CylinderPosition b, float t) =>
            new CylinderPosition(
                a.Angle + CylinderSpace.ShortestDelta(a.Angle, b.Angle) * t,
                Mathf.Lerp(a.Height, b.Height, t));

        public Vector3 ToWorld(float radius) => CylinderSpace.ToWorld(Angle, Height, radius);
    }
}
