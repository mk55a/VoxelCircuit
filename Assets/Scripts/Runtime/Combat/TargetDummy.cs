using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class TargetDummy : MonoBehaviour, IDamageable
    {
        static readonly int s_baseColorId = Shader.PropertyToID("_BaseColor");
        const float FlashTime = 0.08f;

        [SerializeField] private float _angleDegrees;
        [SerializeField] private float _height;
        [SerializeField] private float _radius = 1f;
        [SerializeField, Min(1)] private int _maxHealth = 5;
        [SerializeField] private float _respawnDelay = 1.5f;
        [SerializeField] private Renderer _body;

        ITargetRegistry _registry;
        MaterialPropertyBlock _block;
        Color _baseColor;
        int _health;
        float _respawnAt;
        float _flashLeft;

        public bool IsAlive => _health > 0;
        public Vector3 Position => transform.position;
        public float Radius => _radius;

        void Awake()
        {
            _registry = ServiceLocator.Get<ITargetRegistry>();
            var cylinder = ServiceLocator.Get<CylinderConfig>();

            float angle = _angleDegrees * Mathf.Deg2Rad;
            transform.position = CylinderSpace.ToWorld(angle, _height, cylinder.Radius);
            transform.rotation = Quaternion.LookRotation(CylinderSpace.Tangent(angle), Vector3.up);

            _block = new MaterialPropertyBlock();
            _baseColor = _body.sharedMaterial.GetColor(s_baseColorId);
            _health = _maxHealth;
        }

        void OnEnable() { if (IsAlive) _registry.Register(this); }
        void OnDisable() => _registry.Unregister(this);

        public void TakeHit(int damage)
        {
            if (!IsAlive) return;
            _health -= damage;
            _flashLeft = FlashTime;
            if (_health > 0) return;

            _registry.Unregister(this);
            _body.enabled = false;
            _respawnAt = Time.time + _respawnDelay;
        }

        void Update()
        {
            if (!IsAlive)
            {
                if (Time.time >= _respawnAt) Respawn();
                return;
            }
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            _block.SetColor(s_baseColorId, Color.Lerp(_baseColor, Color.white, _flashLeft / FlashTime));
            _body.SetPropertyBlock(_block);
        }

        void Respawn()
        {
            _health = _maxHealth;
            _body.enabled = true;
            _registry.Register(this);
        }
    }
}
