using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    public sealed class Chaser : MonoBehaviour, IDamageable, IContactHazard
    {
        [Header("Configuration")]
        [SerializeField] private ChaserTuning _tuning;
        [SerializeField] private MonoBehaviour _target;
        [SerializeField] private float _startAngleDegrees;
        [SerializeField] private float _startHeight;
        [SerializeField] private float _startHeadingDegrees;

        private ICylinderTarget _player;
        private ITargetRegistry _targets;
        private CylinderConfig _cylinder;
        private CylinderPosition _position;
        private float _heading;
        private int _health;
        private ContactHazardRegistry _hazards;

        public bool IsAlive => _health > 0;
        public Vector3 Position => transform.position;
        public float Radius => _tuning.HitRadius;
        public CylinderPosition PreviousContactPosition { get; private set; }
        public CylinderPosition ContactPosition => _position;

        private void Awake()
        {
            _player = _target as ICylinderTarget;
            if (_tuning == null || _player == null)
            {
                Debug.LogError("Chaser requires tuning and a player target implementing ICylinderTarget.", this);
                enabled = false;
                return;
            }

            _targets = ServiceLocator.Get<ITargetRegistry>();
            _hazards = ServiceLocator.Get<ContactHazardRegistry>();
            _cylinder = ServiceLocator.Get<CylinderConfig>();
            if (_cylinder.radius <= 0f || _cylinder.minHeight >= _cylinder.maxHeight)
            {
                Debug.LogError("Chaser requires a positive radius and valid height bounds.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!enabled || _targets == null)
            {
                return;
            }

            _position = new CylinderPosition(_startAngleDegrees * Mathf.Deg2Rad,
                Mathf.Clamp(_startHeight, _cylinder.minHeight, _cylinder.maxHeight));
            _heading = _startHeadingDegrees;
            PreviousContactPosition = _position;
            _health = _tuning.Health;
            ApplyPosition();
            _targets.Register(this);
            _hazards.Register(this);
        }

        private void Update()
        {
            PreviousContactPosition = _position;
            var targetPosition = new CylinderPosition(_player.Angle, _player.Height);
            _heading = ChaserSteering.TurnWithinBounds(_heading, _position, targetPosition,
                _cylinder.radius, _tuning.Speed, _tuning.TurnSpeed, Time.deltaTime,
                _cylinder.minHeight, _cylinder.maxHeight);
            _position = ChaserSteering.MoveWithinBounds(_position, _heading, _tuning.Speed,
                _cylinder.radius, Time.deltaTime, _cylinder.minHeight, _cylinder.maxHeight);
            ApplyPosition();
        }

        private void OnDisable()
        {
            _health = 0;
            _targets?.Unregister(this);
            _hazards?.Unregister(this);
        }

        public void TakeHit(int damage)
        {
            if (!IsAlive || damage <= 0)
            {
                return;
            }

            _health -= damage;
            if (_health <= 0)
            {
                gameObject.SetActive(false);
            }
        }

        public void DestroyOnContact()
        {
            if (IsAlive)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyPosition()
        {
            transform.position = _position.ToWorld(_cylinder.radius);
            float heading = _heading * Mathf.Deg2Rad;
            Vector3 direction = -CylinderSpace.Tangent(_position.Angle) * Mathf.Cos(heading)
                + Vector3.up * Mathf.Sin(heading);
            transform.rotation = Quaternion.LookRotation(direction, CylinderSpace.Outward(_position.Angle));
        }
    }
}
