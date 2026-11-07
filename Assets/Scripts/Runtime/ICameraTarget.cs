using System;

namespace CylinderShooter
{
    /// <summary>What the camera needs to know about whatever it follows.</summary>
    public interface ICameraTarget
    {
        /// <summary>Radians, wrapped.</summary>
        float Angle { get; }
        float Height { get; }
        /// <summary>Screen-right velocity in world units/second.</summary>
        float HorizontalVelocity { get; }
        bool IsBoosting { get; }
        event Action<bool> BoostChanged;
    }
}
