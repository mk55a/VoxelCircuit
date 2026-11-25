using System;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>An explosion: where it happened, how big it was, and whether a shot set it off.</summary>
    public readonly struct BlastEvent
    {
        public BlastEvent(Vector3 position, float radius, bool causedByShot)
        {
            Position = position;
            Radius = radius;
            CausedByShot = causedByShot;
        }

        /// <summary>World centre of the blast.</summary>
        public Vector3 Position { get; }

        /// <summary>Blast radius in metres; effects size themselves from it so the visible ring matches the danger zone.</summary>
        public float Radius { get; }

        /// <summary>True when the player shot it while it was armed (a chain reaction), false when the fuse ran out.</summary>
        public bool CausedByShot { get; }
    }

    /// <summary>
    /// Anything that explodes. Blast effects, screen shake or audio listen here, so the exploding enemy
    /// doesn't need to know about them (same idea as <see cref="IDestructible"/>).
    /// </summary>
    public interface IBlastSource
    {
        /// <summary>Raised once per explosion, before the object deactivates.</summary>
        event Action<BlastEvent> Detonated;
    }

    /// <summary>Read by visuals (blinking lamps, warning sounds) that show a fuse counting down.</summary>
    public interface IFuseSource
    {
        bool IsArmed { get; }

        /// <summary>0 when armed, 1 at the explosion.</summary>
        float FuseProgress { get; }

        /// <summary>1 while the warning lamp is lit in its blink, 0 while dark.</summary>
        float BlinkLevel { get; }
    }
}
