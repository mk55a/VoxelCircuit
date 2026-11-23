using CylinderShooter.Core;

namespace CylinderShooter.Combat
{
    public interface IAimSource
    {
        CylinderPosition Position { get; }
        /// <summary>+1 facing screen-right, -1 facing screen-left.</summary>
        float FacingSign { get; }
        float HorizontalVelocity { get; }
    }

    public interface IFireInput
    {
        /// <summary>-1 fire screen-left, +1 fire screen-right, 0 not firing. Independent of where the ship faces.</summary>
        int FireDirection { get; }
    }

    public interface IWeapon
    {
        /// <param name="direction">Screen direction to fire in: negative = left, positive = right.</param>
        bool TryFire(float direction);
    }

    public interface IBulletPool
    {
        Bullet Get();
    }
}
