using UnityEngine;

namespace CylinderShooter.Ship
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Player Life Tuning")]
    public sealed class PlayerLifeTuning : ScriptableObject
    {
        [SerializeField, Min(0)] private int _startingLives = 3;
        [SerializeField, Min(0f)] private float _respawnDelay = 10f;
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 2f;
        [SerializeField, Min(0.01f)] private float _contactRadius = 0.8f;

        public int StartingLives => _startingLives;
        public float RespawnDelay => _respawnDelay;
        public float InvulnerabilityDuration => _invulnerabilityDuration;
        public float ContactRadius => _contactRadius;
    }
}
