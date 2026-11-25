using System;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Formations
{
    /// <summary>
    /// An enemy that can fly in a formation and be reused from a pool (Chaser, Bomber, later others).
    /// Spawners work with this instead of a concrete enemy type, so a new enemy needs no spawner changes.
    /// </summary>
    public interface IFormationMember
    {
        /// <summary>The member's GameObject (implemented automatically by MonoBehaviour).</summary>
        GameObject gameObject { get; }

        /// <summary>True when the prefab is set up well enough to spawn (tuning assigned and valid).</summary>
        bool HasValidTuning { get; }

        /// <summary>Hit radius in metres, used by spawners to keep the formation out of view.</summary>
        float Radius { get; }

        /// <summary>The formation it is flying in, or null once it has broken off (or was placed alone).</summary>
        FormationMotion Formation { get; }

        /// <summary>
        /// Called on an inactive pooled instance just before it is activated. <paramref name="onReturned"/> is
        /// called once when it deactivates (killed or cleared) so the pool can take it back.
        /// </summary>
        void PrepareSpawn(ShipController target, FormationMotion formation, int slot, Action<IFormationMember> onReturned);
    }
}
