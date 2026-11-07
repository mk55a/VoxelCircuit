using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Cam
{
    /// <summary>Sits outside the ring at the target's angle, so the world reads as a flat side-scroller.</summary>
    public sealed class CylinderFollowCamera : MonoBehaviour
    {
        [SerializeField] MonoBehaviour target;
        [SerializeField] CylinderConfig cylinder;
        [SerializeField] float distance = 22f;
        [SerializeField] float heightFollow = 0.6f;
        [SerializeField] float heightSmoothing = 6f;
        [SerializeField] float lookAheadSeconds = 0.15f;
        [SerializeField, Min(0.01f)] float lookAheadSmoothing = 10f;

        ICameraTarget _target;
        float _height;
        float _lookAhead;

        void Awake()
        {
            _target = target as ICameraTarget;
            if (_target == null) Debug.LogError("Camera target must implement ICameraTarget", this);
        }

        void Start()
        {
            _height = _target.Height * heightFollow;
            _lookAhead = _target.HorizontalVelocity * lookAheadSeconds;
        }

        void LateUpdate()
        {
            if (_target == null) return;

            _height = Mathf.Lerp(_height, _target.Height * heightFollow,
                1f - Mathf.Exp(-heightSmoothing * Time.deltaTime));

            // Screen-right is -angle, so leading in the travel direction means subtracting.
            float targetLookAhead = _target.HorizontalVelocity * lookAheadSeconds;
            _lookAhead = Mathf.Lerp(_lookAhead, targetLookAhead,
                1f - Mathf.Exp(-lookAheadSmoothing * Time.deltaTime));
            float angle = _target.Angle - _lookAhead / cylinder.radius;
            var outward = CylinderSpace.Outward(angle);
            transform.position = new Vector3(0f, _height, 0f) + outward * (cylinder.radius + distance);
            transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
        }
    }
}
