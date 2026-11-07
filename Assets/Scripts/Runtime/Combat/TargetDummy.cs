using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class TargetDummy : MonoBehaviour, IDamageable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        const float FlashTime = 0.08f;

        [SerializeField] float angleDegrees;
        [SerializeField] float height;
        [SerializeField] float radius = 1f;
        [SerializeField, Min(1)] int maxHealth = 5;
        [SerializeField] float respawnDelay = 1.5f;
        [SerializeField] Renderer body;

        ITargetRegistry _registry;
        MaterialPropertyBlock _block;
        Color _baseColor;
        int _health;
        float _respawnAt;
        float _flashLeft;

        public bool IsAlive => _health > 0;
        public Vector3 Position => transform.position;
        public float Radius => radius;

        void Awake()
        {
            _registry = ServiceLocator.Get<ITargetRegistry>();
            var cylinder = ServiceLocator.Get<CylinderConfig>();

            float angle = angleDegrees * Mathf.Deg2Rad;
            transform.position = CylinderSpace.ToWorld(angle, height, cylinder.radius);
            transform.rotation = Quaternion.LookRotation(CylinderSpace.Tangent(angle), Vector3.up);

            _block = new MaterialPropertyBlock();
            _baseColor = body.sharedMaterial.GetColor(BaseColorId);
            _health = maxHealth;
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
            body.enabled = false;
            _respawnAt = Time.time + respawnDelay;
        }

        void Update()
        {
            if (!IsAlive)
            {
                if (Time.time >= _respawnAt) Respawn();
                return;
            }
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            _block.SetColor(BaseColorId, Color.Lerp(_baseColor, Color.white, _flashLeft / FlashTime));
            body.SetPropertyBlock(_block);
        }

        void Respawn()
        {
            _health = maxHealth;
            body.enabled = true;
            _registry.Register(this);
        }
    }
}
