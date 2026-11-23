using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class BulletPool : MonoBehaviour, IBulletPool
    {
        [SerializeField] private Bullet _prefab;
        [SerializeField, Min(1)] private int _prewarm = 256;

        ObjectPool<Bullet> _pool;

        public void Initialize(float cylinderRadius, ITargetRegistry targets)
        {
            _pool = new ObjectPool<Bullet>(
                create: () =>
                {
                    var bullet = Instantiate(_prefab, transform);
                    bullet.Initialize(cylinderRadius, targets, Return);
                    return bullet;
                },
                onGet: b => b.gameObject.SetActive(true),
                onRelease: b => b.gameObject.SetActive(false),
                prewarm: _prewarm);
        }

        public Bullet Get() => _pool.Get();

        void Return(Bullet bullet) => _pool.Release(bullet);
    }
}
