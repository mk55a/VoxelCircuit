namespace CylinderShooter
{
    /// <summary>Anything with an engine that an exhaust can react to (the player ship now, enemies later).</summary>
    public interface IThrusterSource
    {
        /// <summary>Current speed in world units/second (magnitude, direction does not matter).</summary>
        float Speed { get; }

        /// <summary>Normal top speed.</summary>
        float CruiseSpeed { get; }

        /// <summary>Top speed while boosting.</summary>
        float BoostSpeed { get; }

        bool IsBoosting { get; }
    }
}
