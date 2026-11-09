using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// Add next to any IDestructible. When it is destroyed, plays an energy pulse sized to its model.
    /// The size is measured once from the model's enabled renderers, so no per-enemy tuning is needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PulseOnDestroyed : MonoBehaviour
    {
        [SerializeField] private PulseProfile _profile;
        [Tooltip("Optional: the model to measure. Leave empty to measure every enabled renderer under this object.")]
        [SerializeField] private Transform _model;

        private IDestructible _destructible;
        private float _radius;

        private void Awake()
        {
            _destructible = GetComponent<IDestructible>();
            if (_destructible == null || _profile == null)
            {
                Debug.LogError("PulseOnDestroyed requires an IDestructible on the same object and a pulse profile.", this);
                enabled = false;
                return;
            }

            _radius = MeasureRadius(_model != null ? _model : transform);
            if (_radius <= 0f)
            {
                Debug.LogError("PulseOnDestroyed found no enabled renderer to size the pulse from.", this);
                enabled = false;
                return;
            }
            _destructible.Destroyed += HandleDestroyed;
        }

        private void OnDestroy()
        {
            if (_destructible != null) _destructible.Destroyed -= HandleDestroyed;
        }

        private void HandleDestroyed(DestructionEvent destruction)
        {
            if (!ServiceLocator.TryGet<IPulseEmitter>(out var pulses)) return;
            pulses.Emit(transform.position, _radius, _profile);
        }

        private float MeasureRadius(Transform root)
        {
            float radius = 0f;
            var toOrigin = Matrix4x4.Translate(-transform.position);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
                radius = Mathf.Max(radius, PulseMath.BoundingRadius(renderer.localBounds,
                    toOrigin * renderer.transform.localToWorldMatrix));
            }
            return radius;
        }
    }
}
