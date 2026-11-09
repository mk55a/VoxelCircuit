using System;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    public sealed class Bullet : MonoBehaviour
    {
        float _cylinderRadius;
        ITargetRegistry _targets;
        Action<Bullet> _release;

        CylinderPosition _position;
        Vector2 _velocity;
        float _lifeLeft;
        float _hitRadius;
        int _damage;
        bool _active;

        [SerializeField] private TrailRenderer _trail; 
        
        public void Initialize(float cylinderRadius, ITargetRegistry targets, Action<Bullet> release)
        {
            _cylinderRadius = cylinderRadius;
            _targets = targets;
            _release = release;
        }

        /// <summary>Surface velocity: x screen-right, y up (world units/second).</summary>
        public void Launch(CylinderPosition start, Vector2 surfaceVelocity, float lifetime, float hitRadius, int damage)
        {
            _position = start;
            _velocity = surfaceVelocity;
            _lifeLeft = lifetime;
            _hitRadius = hitRadius;
            _damage = damage;
            _active = true;
            ApplyTransform();
            if (_trail != null) _trail.Clear();  
        }

        void Update()
        {
            if (!_active) return;

            float dt = Time.deltaTime;
            _lifeLeft -= dt;
            if (_lifeLeft <= 0f) { Despawn(); return; }

            Vector3 previous = transform.position;
            _position = _position.Moved(-_velocity.x * dt / _cylinderRadius, _velocity.y * dt);
            ApplyTransform();

            TestHits(previous, transform.position);
        }

        void TestHits(Vector3 from, Vector3 to)
        {
            var targets = _targets.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!target.IsAlive || !Overlap.SegmentSphere(from, to, _hitRadius, target.Position, target.Radius)) continue;
                target.TakeHit(_damage);
                Despawn();
                return;
            }
        }

        void ApplyTransform()
        {
            transform.position = _position.ToWorld(_cylinderRadius);
            Vector3 dir = -CylinderSpace.Tangent(_position.Angle) * _velocity.x + Vector3.up * _velocity.y;
            if (dir.sqrMagnitude > 1e-6f)
            {
                Vector3 forward = dir.normalized;
                // Side-on to the camera, matching the ship and enemies.
                Vector3 up = Vector3.Cross(forward, CylinderSpace.Outward(_position.Angle));
                transform.rotation = Quaternion.LookRotation(forward, up);
            }
        }
        
        void Despawn()
        {
            _active = false;
            _release(this);
        }
    }
}
