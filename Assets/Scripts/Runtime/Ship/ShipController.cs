using System;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Ship
{
    public sealed class ShipController : MonoBehaviour, ICameraTarget, IAimSource
    {
        [SerializeField] CylinderConfig cylinder;
        [SerializeField] ShipTuning tuning;
        [SerializeField] MonoBehaviour inputSource;
        [SerializeField] float startAngleDegrees;
        [SerializeField] PlayerLife _life;

        ShipMotor _motor;
        ShipFacing _facing;
        BoostAbility _boost;
        IShipInput _input;
        CylinderPosition _previous;
        CylinderPosition _current;

        public event Action<bool> BoostChanged;

        public float Angle => Interpolated.Angle;
        public float Height => Interpolated.Height;
        public float HorizontalVelocity => _motor.Velocity.x;
        public bool IsBoosting => _boost.IsActive;
        public float Facing => _facing.Value;
        public CylinderPosition Position => Interpolated;
        public float FacingSign => _facing.Value >= 0f ? 1f : -1f;
        public float NormalizedVelocity => Mathf.Clamp(_motor.Velocity.x / tuning.MaxSpeed, -1.5f, 1.5f);
        public ShipTuning Tuning => tuning;

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
            _input = inputSource as IShipInput;
            if (_input == null) Debug.LogError("ShipController needs an IShipInput assigned.", this);
            _motor = new ShipMotor(tuning);
            _facing = new ShipFacing();
            _boost = new BoostAbility(tuning);
            _boost.ActiveChanged += active => BoostChanged?.Invoke(active);
            _current = _previous = new CylinderPosition(startAngleDegrees * Mathf.Deg2Rad, 0f);
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
            _facing.Update(move.x, tuning.TurnTime, dt);
            _motor.Step(move, _boost.IsActive, dt);

            _previous = _current;
            // Increasing angle runs screen-left for a camera outside the ring, so screen-right is -angle.
            float deltaAngle = -CylinderSpace.ArcToAngle(_motor.Velocity.x * dt, cylinder.radius);
            var next = _current.Moved(deltaAngle, _motor.Velocity.y * dt);

            float clamped = Mathf.Clamp(next.Height, cylinder.minHeight, cylinder.maxHeight);
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
            transform.position = p.ToWorld(cylinder.radius);

            // Screen-right is -tangent; face along it, with the model's up pointing away from the axis.
            var right = -CylinderSpace.Tangent(p.Angle);
            // The flip swings the nose through the cylinder axis direction so the turn reads on screen.
            var outward = CylinderSpace.Outward(p.Angle);
            float f = _facing.Value;
            var forward = right * f - outward * Mathf.Sqrt(Mathf.Max(0f, 1f - f * f));
            transform.rotation = Quaternion.LookRotation(forward, outward);
        }
    }
}
