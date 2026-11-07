using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Ship Tuning")]
    public sealed class ShipTuning : ScriptableObject, IShipTuning
    {
        [Header("Movement")]
        [SerializeField] float thrust = 70f;
        [SerializeField] float verticalThrust = 50f;
        [SerializeField] float drag = 3f;
        [SerializeField] float maxSpeed = 22f;
        [SerializeField] float maxVerticalSpeed = 12f;
        [SerializeField] float reverseThrustMultiplier = 2f;
        [SerializeField] float turnTime = 0.15f;

        [Header("Boost")]
        [SerializeField] float boostSpeedMultiplier = 1.8f;
        [SerializeField] float boostThrustMultiplier = 2f;
        [SerializeField] float boostDuration = 0.6f;
        [SerializeField] float boostCooldown = 2f;
        [Tooltip("Minimum rate at which speed above the normal maximum returns to it after boosting.")]
        [SerializeField, Min(0.01f)] float boostRecoveryDeceleration = 30f;

        [Header("Visual")]
        [SerializeField] float maxBankAngle = 35f;
        [SerializeField] float bankSmoothing = 10f;

        public float Thrust => thrust;
        public float VerticalThrust => verticalThrust;
        public float Drag => drag;
        public float MaxSpeed => maxSpeed;
        public float MaxVerticalSpeed => maxVerticalSpeed;
        public float ReverseThrustMultiplier => reverseThrustMultiplier;
        public float TurnTime => turnTime;
        public float BoostSpeedMultiplier => boostSpeedMultiplier;
        public float BoostThrustMultiplier => boostThrustMultiplier;
        public float BoostDuration => boostDuration;
        public float BoostCooldown => boostCooldown;
        public float BoostRecoveryDeceleration => boostRecoveryDeceleration;
        public float MaxBankAngle => maxBankAngle;
        public float BankSmoothing => bankSmoothing;
    }
}
