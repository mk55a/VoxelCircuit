using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Add next to any IDestructible (Chaser, Drifter, the player ship, props). When it is destroyed, its
    /// cubes are thrown out as debris, pushed away from whatever destroyed it. Knows nothing about the
    /// concrete type it is attached to.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShatterOnDestroyed : MonoBehaviour
    {
        [Tooltip("The model's *_Voxels.json, e.g. Chaser_Voxels.json.")]
        [SerializeField] private TextAsset _voxelData;
        [Tooltip("The imported model child. Leave empty if the model sits at this object's origin.")]
        [SerializeField] private Transform _model;
        [SerializeField] private DebrisProfile _profile;

        private IDestructible _destructible;
        private VoxelShape _shape;

        private void Awake()
        {
            _destructible = GetComponent<IDestructible>();
            if (_destructible == null || _voxelData == null || _profile == null)
            {
                Debug.LogError("ShatterOnDestroyed requires an IDestructible on the same object, voxel data and a debris profile.", this);
                enabled = false;
                return;
            }

            if (_model == null) _model = transform;
            _shape = VoxelShape.Load(_voxelData);
            _destructible.Destroyed += HandleDestroyed;
        }

        private void OnDestroy()
        {
            if (_destructible != null) _destructible.Destroyed -= HandleDestroyed;
        }

        private void HandleDestroyed(DestructionEvent destruction)
        {
            if (!ServiceLocator.TryGet<IDebrisEmitter>(out var debris)) return;

            Vector3 push = destruction.HasInstigator
                ? DebrisMath.AwayAlongSurface(destruction.InstigatorAngle, destruction.Position.Angle)
                : Vector3.zero;
            debris.Emit(_shape, _model, push, _profile);
        }
    }
}
