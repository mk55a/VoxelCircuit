using UnityEngine;

namespace CylinderShooter.Enemies
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Drifter Tuning")]
    public sealed class DrifterTuning : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _speed = 4f;

        [Header("Combat")]
        [SerializeField, Min(0.01f)] private float _hitRadius = 1f;

        public float Speed => _speed;
        public float HitRadius => _hitRadius;
    }
}
