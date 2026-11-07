using UnityEngine;

namespace CylinderShooter.Cam
{
    /// <summary>Widens the field of view while the target boosts.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FovBoostEffect : MonoBehaviour
    {
        [SerializeField] MonoBehaviour target;
        [SerializeField] float boostFovAdd = 18f;
        [SerializeField] float smoothing = 8f;

        Camera _camera;
        ICameraTarget _target;
        float _baseFov;
        bool _boosting;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseFov = _camera.fieldOfView;
            _target = target as ICameraTarget;
        }

        void OnEnable() { if (_target != null) _target.BoostChanged += OnBoostChanged; }
        void OnDisable() { if (_target != null) _target.BoostChanged -= OnBoostChanged; }

        void OnBoostChanged(bool active) => _boosting = active;

        void LateUpdate()
        {
            float goal = _baseFov + (_boosting ? boostFovAdd : 0f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, goal, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}
