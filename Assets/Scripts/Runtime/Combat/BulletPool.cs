using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class BulletPool : MonoBehaviour, IBulletPool
    {
        [SerializeField] Bullet prefab;
        [SerializeField, Min(1)] int prewarm = 256;

        ObjectPool<Bullet> _pool;

        public void Initialize(float cylinderRadius, ITargetRegistry targets)
        {
            _pool = new ObjectPool<Bullet>(
                create: () =>
                {
                    var bullet = Instantiate(prefab, transform);
                    bullet.Initialize(cylinderRadius, targets, Return);
                    return bullet;
                },
                onGet: b => b.gameObject.SetActive(true),
                onRelease: b => b.gameObject.SetActive(false),
                prewarm: prewarm);
        }

        public Bullet Get() => _pool.Get();

        void Return(Bullet bullet) => _pool.Release(bullet);
    }
}
