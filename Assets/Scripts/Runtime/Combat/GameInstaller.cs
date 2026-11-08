using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private BulletPool _bulletPool;

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
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_targets);
            ServiceLocator.Unregister<IBulletPool>(_bulletPool);
            ServiceLocator.Unregister(_hazards);
        }
    }
}
