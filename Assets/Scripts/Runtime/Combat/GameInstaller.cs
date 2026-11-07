using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    /// <summary>Composition root: builds shared services and registers them before anything else wakes up.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] CylinderConfig cylinder;
        [SerializeField] BulletPool bulletPool;

        TargetRegistry _targets;

        void Awake()
        {
            _targets = new TargetRegistry();
            bulletPool.Initialize(cylinder.radius, _targets);

            ServiceLocator.Register(cylinder);
            ServiceLocator.Register<ITargetRegistry>(_targets);
            ServiceLocator.Register<IBulletPool>(bulletPool);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_targets);
            ServiceLocator.Unregister<IBulletPool>(bulletPool);
        }
    }
}
