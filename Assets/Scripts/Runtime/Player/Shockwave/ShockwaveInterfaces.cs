using System;
using CylinderShooter.Core;

namespace CylinderShooter.Combat
{
    /// <summary>Something that can release a shockwave (the player's special move now, maybe pickups or bosses later).</summary>
    public interface IShockwaveEmitter
    {
        bool IsActive { get; }

        /// <summary>The wave starts forming around the player (the build-up).</summary>
        event Action Triggered;

        /// <summary>The build-up is complete and the wave is thrown outward.</summary>
        event Action Released;

        /// <summary>The two halves have met on the far side; the wave is gone.</summary>
        event Action Finished;

        /// <summary>Starts a wave at this position. Returns false if one is already running.</summary>
        bool TryTrigger(CylinderPosition origin);
    }

    public interface IShockwaveInput
    {
        /// <summary>True once per press; reading consumes it.</summary>
        bool ConsumeShockwave();
    }
}
