namespace CylinderShooter.Core
{
    /// <summary>Read-only ship tuning values. Speeds are world units/second along the surface.</summary>
    public interface IShipTuning
    {
        float Thrust { get; }
        float VerticalThrust { get; }
        float Drag { get; }
        float MaxSpeed { get; }
        float MaxVerticalSpeed { get; }
        /// <summary>Extra thrust multiplier when input opposes current velocity (snappier reversals).</summary>
        float ReverseThrustMultiplier { get; }
        float TurnTime { get; }
        float BoostSpeedMultiplier { get; }
        float BoostThrustMultiplier { get; }
        float BoostDuration { get; }
        float BoostCooldown { get; }
    }
}
