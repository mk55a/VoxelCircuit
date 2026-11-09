using System;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    public sealed class Drifter : MonoBehaviour, IDamageable, IContactHazard, IDestructible
    {
        [Header("Configuration")]
        [SerializeField] private DrifterTuning _tuning;
        [SerializeField] private float _startAngleDegrees;
        [SerializeField] private float _startHeight;
        [SerializeField] private bool _moveRight = true;

        private ITargetRegistry _targets;
        private CylinderConfig _cylinder;
        private CylinderPosition _position;
        private ContactHazardRegistry _hazards;

        public bool IsAlive { get; private set; }

        /// <summary>Shot down or crashed into the player; not raised when cleared or returned to a pool.</summary>
        public event Action<DestructionEvent> Destroyed;
        public Vector3 Position => transform.position;
        public float Radius => _tuning.HitRadius;
        public CylinderPosition PreviousContactPosition { get; private set; }
        public CylinderPosition ContactPosition => _position;

        private void Awake()
        {
            if (_tuning == null)
            {
                Debug.LogError("Drifter requires a tuning asset.", this);
                enabled = false;
                return;
            }

            _targets = ServiceLocator.Get<ITargetRegistry>();
            _hazards = ServiceLocator.Get<ContactHazardRegistry>();
            _cylinder = ServiceLocator.Get<CylinderConfig>();
            if (_cylinder.Radius <= 0f)
            {
                Debug.LogError("Drifter requires a positive cylinder radius.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_targets == null || _cylinder == null || _tuning == null || !enabled)
            {
                return;
            }

            _position = new CylinderPosition(_startAngleDegrees * Mathf.Deg2Rad, _startHeight);
            PreviousContactPosition = _position;
            ApplyPosition();
            IsAlive = true;
            _targets.Register(this);
            _hazards.Register(this);
        }

        private void Update()
        {
            PreviousContactPosition = _position;
            float speed = _moveRight ? _tuning.Speed : -_tuning.Speed;
            _position = CylinderMotion.MoveHorizontally(_position, speed, _cylinder.Radius, Time.deltaTime);
            ApplyPosition();
        }

        private void OnDisable()
        {
            IsAlive = false;
            _targets?.Unregister(this);
            _hazards?.Unregister(this);
        }

        public void TakeHit(int damage)
        {
            if (!IsAlive || damage <= 0)
            {
                return;
            }

            DestroyInPlay();
        }

        public void DestroyOnContact()
        {
            if (IsAlive)
            {
                DestroyInPlay();
            }
        }

        private void DestroyInPlay()
        {
            // Drifters don't track the player, so their debris bursts evenly.
            Destroyed?.Invoke(new DestructionEvent(_position));
            gameObject.SetActive(false);
        }

        private void ApplyPosition()
        {
            transform.position = _position.ToWorld(_cylinder.Radius);
            Vector3 direction = CylinderSpace.Tangent(_position.Angle) * (_moveRight ? -1f : 1f);
            // Side-on to the camera: model up = world up.
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
