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
        bool FireHeld { get; }
    }

    public interface IWeapon
    {
        bool TryFire();
    }

    public interface IBulletPool
    {
        Bullet Get();
    }
}
