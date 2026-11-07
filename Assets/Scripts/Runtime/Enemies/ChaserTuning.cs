using UnityEngine;

namespace CylinderShooter.Enemies
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Chaser Tuning")]
    public sealed class ChaserTuning : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float _speed = 7f;
        [SerializeField, Min(0f)] private float _turnSpeed = 100f;

        [Header("Combat")]
        [SerializeField, Min(0.01f)] private float _hitRadius = 1f;
        [SerializeField, Min(1)] private int _health = 2;

        public float Speed => _speed;
        public float TurnSpeed => _turnSpeed;
        public float HitRadius => _hitRadius;
        public int Health => _health;
    }
}
