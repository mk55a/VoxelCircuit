using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Pure fixed-step ship physics in surface space: x = screen-right along the arc, y = up.
    /// Knows nothing about angles, the cylinder, or Unity components.
    /// </summary>
    public sealed class ShipMotor
    {
        readonly IShipTuning _tuning;

        public ShipMotor(IShipTuning tuning) => _tuning = tuning;

        public Vector2 Velocity { get; private set; }

        public void Step(Vector2 input, bool boosting, float dt)
        {
            float thrustMul = boosting ? _tuning.BoostThrustMultiplier : 1f;
            float speedMul = boosting ? _tuning.BoostSpeedMultiplier : 1f;

            float ax = input.x * _tuning.Thrust * thrustMul;
            if (input.x * Velocity.x < 0f) ax *= _tuning.ReverseThrustMultiplier;
            float ay = input.y * _tuning.VerticalThrust;

            if (boosting && Mathf.Approximately(input.x, 0f))
                ax = Mathf.Sign(Velocity.x) * _tuning.Thrust * thrustMul;

            var v = Velocity + new Vector2(ax, ay) * dt;
            v *= Mathf.Exp(-_tuning.Drag * dt);

            float maxX = _tuning.MaxSpeed * speedMul;
            // Preserve boost momentum while gradually restoring the normal speed limit.
            float recoveryLimit = Mathf.Max(maxX,
                Mathf.Abs(Velocity.x) - _tuning.BoostRecoveryDeceleration * dt);
            float minX = Velocity.x < -maxX ? -recoveryLimit : -maxX;
            float upperX = Velocity.x > maxX ? recoveryLimit : maxX;
            v.x = Mathf.Clamp(v.x, minX, upperX);
            v.y = Mathf.Clamp(v.y, -_tuning.MaxVerticalSpeed, _tuning.MaxVerticalSpeed);
            Velocity = v;
        }

        public void StopVertical() => Velocity = new Vector2(Velocity.x, 0f);

        public void Stop() => Velocity = Vector2.zero;
    }
}
