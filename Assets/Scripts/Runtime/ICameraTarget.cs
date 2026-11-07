using System;

namespace CylinderShooter
{
    public interface ICameraTarget : ICylinderTarget
    {
        /// <summary>Screen-right velocity in world units/second.</summary>
        float HorizontalVelocity { get; }
        bool IsBoosting { get; }
        event Action<bool> BoostChanged;
    }
}
