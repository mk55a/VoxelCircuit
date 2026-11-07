using CylinderShooter.Core;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class MainCannon : MonoBehaviour, IWeapon
    {
        [SerializeField] WeaponTuning tuning;
        [SerializeField] MonoBehaviour aimSource;
        [SerializeField] MonoBehaviour fireInput;
        [SerializeField] PlayerLife _life;

        readonly FireRateLimiter _limiter = new FireRateLimiter();
        IAimSource _aim;
        IFireInput _input;
        IBulletPool _pool;
        float _cylinderRadius;

        void Awake()
        {
            _aim = aimSource as IAimSource;
            _input = fireInput as IFireInput;
            if (_aim == null || _input == null)
                Debug.LogError("MainCannon needs an IAimSource and IFireInput assigned.", this);

            _pool = ServiceLocator.Get<IBulletPool>();
            _cylinderRadius = ServiceLocator.Get<CylinderConfig>().radius;
        }

        void Update()
        {
            if (_input.FireHeld) TryFire();
        }

        public bool TryFire()
        {
            if (_life != null && !_life.CanControl) return false;
            if (!_limiter.TryFire(Time.time, tuning.ShotsPerSecond)) return false;

            float dir = _aim.FacingSign;
            var start = _aim.Position.Moved(-dir * tuning.MuzzleOffset / _cylinderRadius, 0f);
            float inherited = Mathf.Max(0f, _aim.HorizontalVelocity * dir) * tuning.InheritVelocity;
            var velocity = new Vector2(dir * (tuning.BulletSpeed + inherited), 0f);

            _pool.Get().Launch(start, velocity, tuning.BulletLifetime, tuning.BulletHitRadius, tuning.Damage);
            return true;
        }
    }
}
