using UnityEngine;

namespace CylinderShooter.Combat
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Weapon Tuning")]
    public sealed class WeaponTuning : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _shotsPerSecond = 12f;
        [SerializeField, Min(1f)] private float _bulletSpeed = 60f;
        [SerializeField, Min(0.1f)] private float _bulletLifetime = 1.5f;
        [SerializeField, Min(0.05f)] private float _bulletHitRadius = 0.4f;
        [SerializeField, Min(1)] private int _damage = 1;
        [SerializeField] private float _muzzleOffset = 1.5f;
        [Tooltip("Fraction of the ship's forward speed added to the bullet")]
        [SerializeField, Range(0f, 1f)] private float _inheritVelocity = 0.5f;

        public float ShotsPerSecond => _shotsPerSecond;
        public float BulletSpeed => _bulletSpeed;
        public float BulletLifetime => _bulletLifetime;
        public float BulletHitRadius => _bulletHitRadius;
        public int Damage => _damage;
        public float MuzzleOffset => _muzzleOffset;
        public float InheritVelocity => _inheritVelocity;
    }
}
