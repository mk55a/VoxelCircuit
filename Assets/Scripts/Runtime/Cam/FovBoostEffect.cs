using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Cam
{
    [RequireComponent(typeof(Camera))]
    public sealed class FovBoostEffect : MonoBehaviour
    {
        [SerializeField] private ShipController _targetSource;
        [SerializeField] private float _boostFovAdd = 18f;
        [SerializeField] private float _smoothing = 8f;

        Camera _camera;
        float _baseFov;
        bool _boosting;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseFov = _camera.fieldOfView;
            if (_targetSource != null) return;
            Debug.LogError("FovBoostEffect requires a ship target.", this);
            enabled = false;
        }

        void OnEnable() { if (_targetSource != null) _targetSource.OnBoostChanged += OnBoostChanged; }
        void OnDisable() { if (_targetSource != null) _targetSource.OnBoostChanged -= OnBoostChanged; }

        void OnBoostChanged(bool active) => _boosting = active;

        void LateUpdate()
        {
            float goal = _baseFov + (_boosting ? _boostFovAdd : 0f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, goal, 1f - Mathf.Exp(-_smoothing * Time.deltaTime));
        }
    }
}
