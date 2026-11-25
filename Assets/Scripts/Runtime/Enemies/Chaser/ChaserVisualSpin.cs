using UnityEngine;

namespace CylinderShooter.Enemies
{
    public sealed class ChaserVisualSpin : MonoBehaviour
    {
        [SerializeField] private Transform _model;
        [Tooltip("Average spin speed in degrees per second. Set to zero to disable the spin.")]
        [SerializeField, Min(0f)] private float _degreesPerSecond = 60f;
        [Tooltip("Random variation in speed for each spawn, in degrees per second.")]
        [SerializeField, Min(0f)] private float _speedVariation = 15f;
        [Tooltip("Axis the model spins around, in the model's local space. (0, 0, 1) = around its nose, like a drill " +
                 "(Chaser). (0, 1, 0) = around its vertical, like a slowly turning mine (Bomber).")]
        [SerializeField] private Vector3 _axis = Vector3.forward;

        private Quaternion _baseRotation;
        private bool _initialized;
        private float _angle;
        private float _speed;

        private void Awake()
        {
            if (_model == null || _model == transform || !_model.IsChildOf(transform))
            {
                Debug.LogError("Chaser visual spin requires a model child.", this);
                enabled = false;
                return;
            }
            _baseRotation = _model.localRotation;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (!enabled) return;

            if (_degreesPerSecond <= 0f)
            {
                _speed = 0f;
                _angle = 0f;
            }
            else
            {
                float variation = Mathf.Max(0f, _speedVariation);
                _speed = Random.Range(Mathf.Max(0f, _degreesPerSecond - variation),
                    _degreesPerSecond + variation);
                _angle = Random.Range(0f, 360f);
            }
            ApplyRotation();
        }

        private void Update()
        {
            _angle = Mathf.Repeat(_angle + _speed * Time.deltaTime, 360f);
            ApplyRotation();
        }

        private void OnDisable()
        {
            if (_initialized && _model != null) _model.localRotation = _baseRotation;
        }

        private void ApplyRotation()
        {
            _model.localRotation = _baseRotation * Quaternion.AngleAxis(_angle,
                _axis.sqrMagnitude > 0f ? _axis : Vector3.forward);
        }
    }
}
