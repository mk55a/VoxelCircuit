namespace CylinderShooter.Core
{
    /// <summary>Shape and timing of a shockwave. Implemented by the ShockwaveProfile asset.</summary>
    public interface IShockwaveSettings
    {
        /// <summary>
        /// Build-up: seconds the wave spends as a slowly growing ring around the player before it is thrown.
        /// It already destroys what it touches while forming.
        /// </summary>
        float FormDuration { get; }

        /// <summary>Radius of the ring the wave starts as, in metres (about half the ship's length).</summary>
        float FormRadius { get; }

        /// <summary>Radius of the ring at the end of the build-up, in metres.</summary>
        float FormDistance { get; }

        /// <summary>Seconds, after the build-up, for the two halves to meet on the far side of the cylinder.</summary>
        float Duration { get; }

        /// <summary>Kill band width behind the front, in metres.</summary>
        float Thickness { get; }

        /// <summary>How far the top and bottom lag behind the middle once the ring has relaxed into "( )", in metres.</summary>
        float CurveDepth { get; }

        /// <summary>
        /// Metres of travel, after the ring has grown taller than the cylinder, over which the round "O" relaxes into
        /// the flatter "( )".
        /// </summary>
        float FlattenDistance { get; }

        /// <summary>1 = constant speed. Higher = thrown out fast at release, then slows.</summary>
        float EaseOut { get; }
    }
}
