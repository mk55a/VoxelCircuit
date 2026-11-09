using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>Plays a short energy pulse (flash, ring, sparks). Implemented by PulseEmitter; resolved through the ServiceLocator.</summary>
    public interface IPulseEmitter
    {
        /// <param name="position">World centre of the pulse.</param>
        /// <param name="radius">Size of the thing that was destroyed, in metres; the profile scales from this.</param>
        /// <param name="profile">How this pulse looks and moves.</param>
        void Emit(Vector3 position, float radius, PulseProfile profile);
    }
}
