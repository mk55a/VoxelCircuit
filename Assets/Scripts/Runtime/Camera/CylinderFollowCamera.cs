using CylinderShooter.Core;
using CylinderShooter.Ship;
using UnityEngine;

namespace CylinderShooter.Cam
{
    /// <summary>Sits outside the ring at the target's angle, so the world reads as a flat side-scroller.</summary>
    public sealed class CylinderFollowCamera : MonoBehaviour
    {
        [SerializeField] private ShipController _targetSource;
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private float _distance = 22f;
        [SerializeField] private float _heightFollow = 0.6f;
        [SerializeField] private float _heightSmoothing = 6f;
        [SerializeField] private float _lookAheadSeconds = 0.15f;
        [SerializeField, Min(0.01f)] private float _lookAheadSmoothing = 10f;

        float _height;
        float _lookAhead;

        void Awake()
        {
            if (_targetSource != null) return;
            Debug.LogError("Camera requires a ship target.", this);
            enabled = false;
        }

        void Start()
        {
            _height = _targetSource.Height * _heightFollow;
            _lookAhead = _targetSource.HorizontalVelocity * _lookAheadSeconds;
        }

        void LateUpdate()
        {
            _height = Mathf.Lerp(_height, _targetSource.Height * _heightFollow,
                1f - Mathf.Exp(-_heightSmoothing * Time.deltaTime));

            // Screen-right is -angle, so leading in the travel direction means subtracting.
            float targetLookAhead = _targetSource.HorizontalVelocity * _lookAheadSeconds;
            _lookAhead = Mathf.Lerp(_lookAhead, targetLookAhead,
                1f - Mathf.Exp(-_lookAheadSmoothing * Time.deltaTime));
            float angle = _targetSource.Angle - _lookAhead / _cylinder.Radius;
            var outward = CylinderSpace.Outward(angle);
            transform.position = new Vector3(0f, _height, 0f) + outward * (_cylinder.Radius + _distance);
            transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
        }
    }
}
