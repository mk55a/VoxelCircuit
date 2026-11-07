using UnityEngine;

namespace CylinderShooter.Ship
{
    public interface IShipInput
    {
        /// <summary>x: screen-right positive, y: up positive. Magnitude up to 1 per axis.</summary>
        Vector2 Move { get; }
        /// <summary>True once per press; reading consumes it.</summary>
        bool ConsumeBoost();
    }
}
