using System;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Anything that can be destroyed in play (enemies, the player ship, props). Listeners such as
    /// destruction effects, score or audio subscribe here instead of depending on a concrete type.
    /// </summary>
    public interface IDestructible
    {
        /// <summary>Raised once per destruction, before the object deactivates.</summary>
        event Action<DestructionEvent> Destroyed;
    }
}
