using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Ship Tuning")]
    public sealed class ShipTuning : ScriptableObject, IShipTuning
    {
        [Header("Movement")]
        [SerializeField] private float _thrust = 70f;
        [SerializeField] private float _verticalThrust = 50f;
        [SerializeField] private float _drag = 3f;
        [SerializeField] private float _maxSpeed = 22f;
        [SerializeField] private float _maxVerticalSpeed = 12f;
        [SerializeField] private float _reverseThrustMultiplier = 2f;
        [SerializeField] private float _turnTime = 0.15f;

        [Header("Boost")]
        [SerializeField] private float _boostSpeedMultiplier = 1.8f;
        [SerializeField] private float _boostThrustMultiplier = 2f;
        [SerializeField] private float _boostDuration = 0.6f;
        [SerializeField] private float _boostCooldown = 2f;
        [Tooltip("Minimum rate at which speed above the normal maximum returns to it after boosting.")]
        [SerializeField, Min(0.01f)] private float _boostRecoveryDeceleration = 30f;

        [Header("Visual")]
        [SerializeField] private float _maxBankAngle = 35f;
        [SerializeField] private float _bankSmoothing = 10f;

        public float Thrust => _thrust;
        public float VerticalThrust => _verticalThrust;
        public float Drag => _drag;
        public float MaxSpeed => _maxSpeed;
        public float MaxVerticalSpeed => _maxVerticalSpeed;
        public float ReverseThrustMultiplier => _reverseThrustMultiplier;
        public float TurnTime => _turnTime;
        public float BoostSpeedMultiplier => _boostSpeedMultiplier;
        public float BoostThrustMultiplier => _boostThrustMultiplier;
        public float BoostDuration => _boostDuration;
        public float BoostCooldown => _boostCooldown;
        public float BoostRecoveryDeceleration => _boostRecoveryDeceleration;
        public float MaxBankAngle => _maxBankAngle;
        public float BankSmoothing => _bankSmoothing;
    }
}
