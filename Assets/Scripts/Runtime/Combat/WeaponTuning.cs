using UnityEngine;

namespace CylinderShooter.Combat
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Weapon Tuning")]
    public sealed class WeaponTuning : ScriptableObject
    {
        [SerializeField, Min(0.1f)] float shotsPerSecond = 12f;
        [SerializeField, Min(1f)] float bulletSpeed = 60f;
        [SerializeField, Min(0.1f)] float bulletLifetime = 1.5f;
        [SerializeField, Min(0.05f)] float bulletHitRadius = 0.4f;
        [SerializeField, Min(1)] int damage = 1;
        [SerializeField] float muzzleOffset = 1.5f;
        [Tooltip("Fraction of the ship's forward speed added to the bullet")]
        [SerializeField, Range(0f, 1f)] float inheritVelocity = 0.5f;

        public float ShotsPerSecond => shotsPerSecond;
        public float BulletSpeed => bulletSpeed;
        public float BulletLifetime => bulletLifetime;
        public float BulletHitRadius => bulletHitRadius;
        public int Damage => damage;
        public float MuzzleOffset => muzzleOffset;
        public float InheritVelocity => inheritVelocity;
    }
}
