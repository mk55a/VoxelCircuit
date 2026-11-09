using CylinderShooter.Core;
using CylinderShooter.Effects;
using UnityEngine;

namespace CylinderShooter.Combat
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private BulletPool _bulletPool;
        [Tooltip("Optional: shared voxel debris for destruction effects.")]
        [SerializeField] private VoxelDebris _debris;
        [Tooltip("Optional: shared energy pulses for destruction effects.")]
        [SerializeField] private PulseEmitter _pulses;

        TargetRegistry _targets;
        ContactHazardRegistry _hazards;

        void Awake()
        {
            _targets = new TargetRegistry();
            _hazards = new ContactHazardRegistry();
            _bulletPool.Initialize(_cylinder.Radius, _targets);

            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(_targets);
            ServiceLocator.Register<IBulletPool>(_bulletPool);
            ServiceLocator.Register(_hazards);

            if (_debris != null)
            {
                _debris.Initialize(_cylinder);
                ServiceLocator.Register<IDebrisEmitter>(_debris);
            }
            if (_pulses != null) ServiceLocator.Register<IPulseEmitter>(_pulses);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_targets);
            ServiceLocator.Unregister<IBulletPool>(_bulletPool);
            ServiceLocator.Unregister(_hazards);
            if (_debris != null) ServiceLocator.Unregister<IDebrisEmitter>(_debris);
            if (_pulses != null) ServiceLocator.Unregister<IPulseEmitter>(_pulses);
        }
    }
}
