using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>Throws a model's cubes out as debris. Implemented by VoxelDebris; resolved through the ServiceLocator.</summary>
    public interface IDebrisEmitter
    {
        /// <param name="shape">The cubes the model is made of.</param>
        /// <param name="model">The model transform the cube positions are relative to.</param>
        /// <param name="push">Unit direction to push debris (e.g. away from the player); zero for an even burst.</param>
        /// <param name="profile">How this debris looks and moves.</param>
        void Emit(VoxelShape shape, Transform model, Vector3 push, DebrisProfile profile);
    }
}
