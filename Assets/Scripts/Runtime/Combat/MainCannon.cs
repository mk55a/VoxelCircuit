using CylinderShooter.Core;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class MainCannon : MonoBehaviour, IWeapon
    {
        [SerializeField] private WeaponTuning _tuning;
        [SerializeField] private ShipController _aimSource;
        [SerializeField] private MonoBehaviour _fireInput;
        [SerializeField] private PlayerLife _life;

        readonly FireRateLimiter _limiter = new FireRateLimiter();
        IFireInput _input;
        IBulletPool _pool;
        float _cylinderRadius;

        void Awake()
        {
            _input = _fireInput as IFireInput;
            if (_aimSource == null || _input == null)
            {
                Debug.LogError("MainCannon needs a ship aim source and an IFireInput assigned.", this);
                enabled = false;
                return;
            }

            _pool = ServiceLocator.Get<IBulletPool>();
            _cylinderRadius = ServiceLocator.Get<CylinderConfig>().Radius;
        }

        void Update()
        {
            int direction = _input.FireDirection;
            if (direction != 0) TryFire(direction);
        }

        /// <summary>Fires one shot to screen-left (negative) or screen-right (positive), whichever way the ship faces.</summary>
        public bool TryFire(float direction)
        {
            if (Mathf.Approximately(direction, 0f)) return false;
            if (_life != null && !_life.CanControl) return false;
            if (!_limiter.TryFire(Time.time, _tuning.ShotsPerSecond)) return false;

            float dir = Mathf.Sign(direction);
            var start = _aimSource.Position.Moved(-dir * _tuning.MuzzleOffset / _cylinderRadius, 0f);
            float inherited = Mathf.Max(0f, _aimSource.HorizontalVelocity * dir) * _tuning.InheritVelocity;
            var velocity = new Vector2(dir * (_tuning.BulletSpeed + inherited), 0f);

            _pool.Get().Launch(start, velocity, _tuning.BulletLifetime, _tuning.BulletHitRadius, _tuning.Damage);
            return true;
        }
    }
}
