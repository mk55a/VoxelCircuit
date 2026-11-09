using System;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    public sealed class ShipController : MonoBehaviour, ICameraTarget, IAimSource
    {
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private ShipTuning _tuning;
        [SerializeField] private MonoBehaviour _inputSource;
        [SerializeField] private float _startAngleDegrees;
        [SerializeField] private PlayerLife _life;

        ShipMotor _motor;
        ShipFacing _facing;
        BoostAbility _boost;
        IShipInput _input;
        CylinderPosition _previous;
        CylinderPosition _current;

        public event Action<bool> OnBoostChanged;

        public float Angle => Interpolated.Angle;
        public float Height => Interpolated.Height;
        public float HorizontalVelocity => _motor.Velocity.x;
        public bool IsBoosting => _boost.IsActive;
        public float Facing => _facing.Value;
        public CylinderPosition Position => Interpolated;
        public float FacingSign => _facing.Value >= 0f ? 1f : -1f;
        public float NormalizedVelocity => Mathf.Clamp(_motor.Velocity.x / _tuning.MaxSpeed, -1.5f, 1.5f);
        public ShipTuning Tuning => _tuning;

        CylinderPosition Interpolated
        {
            get
            {
                float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                return CylinderPosition.Lerp(_previous, _current, alpha);
            }
        }

        void Awake()
        {
            _input = _inputSource as IShipInput;
            if (_input == null) Debug.LogError("ShipController needs an IShipInput assigned.", this);
            _motor = new ShipMotor(_tuning);
            _facing = new ShipFacing();
            _boost = new BoostAbility(_tuning);
            _boost.OnActiveChanged += active => OnBoostChanged?.Invoke(active);
            _current = _previous = new CylinderPosition(_startAngleDegrees * Mathf.Deg2Rad, 0f);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_life != null && !_life.CanControl)
            {
                _input.ConsumeBoost();
                return;
            }
            var move = _input.Move;

            if (_input.ConsumeBoost()) _boost.TryActivate();
            _boost.Tick(dt);
            _facing.Update(move.x, _tuning.TurnTime, dt);
            _motor.Step(move, _boost.IsActive, dt);

            _previous = _current;
            // Increasing angle runs screen-left for a camera outside the ring, so screen-right is -angle.
            float deltaAngle = -CylinderSpace.ArcToAngle(_motor.Velocity.x * dt, _cylinder.Radius);
            var next = _current.Moved(deltaAngle, _motor.Velocity.y * dt);

            float clamped = Mathf.Clamp(next.Height, _cylinder.MinHeight, _cylinder.MaxHeight);
            if (!Mathf.Approximately(clamped, next.Height)) _motor.StopVertical();
            _current = next.WithHeight(clamped);
        }

        public void StopAtCurrentPosition()
        {
            _current = _previous = Interpolated;
            _motor.Stop();
            _boost.Interrupt();
            _input.ConsumeBoost();
        }

        public void ResetAfterRespawn()
        {
            _previous = _current;
            _input.ConsumeBoost();
        }

        void LateUpdate()
        {
            var p = Interpolated;
            transform.position = p.ToWorld(_cylinder.Radius);

            // Screen-right is -tangent. The model's up is world up, so the camera (outside the ring)
            // sees the ship side-on; banking then tilts its top or bottom toward the camera.
            var right = -CylinderSpace.Tangent(p.Angle);
            // The flip yaws the nose away from the camera (into the ring), so mid-turn the rear shows.
            var outward = CylinderSpace.Outward(p.Angle);
            float f = _facing.Value;
            var forward = right * f - outward * Mathf.Sqrt(Mathf.Max(0f, 1f - f * f));
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
