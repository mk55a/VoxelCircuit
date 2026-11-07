using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Enemies
{
    public sealed class Drifter : MonoBehaviour, IDamageable, IContactHazard
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
            if (_cylinder.radius <= 0f)
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
            _position = CylinderMotion.MoveHorizontally(_position, speed, _cylinder.radius, Time.deltaTime);
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

            gameObject.SetActive(false);
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
            Vector3 direction = CylinderSpace.Tangent(_position.Angle) * (_moveRight ? -1f : 1f);
            transform.rotation = Quaternion.LookRotation(direction, CylinderSpace.Outward(_position.Angle));
        }
    }
}
