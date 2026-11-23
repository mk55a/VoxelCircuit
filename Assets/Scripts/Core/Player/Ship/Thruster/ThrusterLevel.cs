using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Smoothed thrust level with separate rise and fall rates: the exhaust can flare up quickly on boost
    /// and settle back slowly. Frame-rate independent (exponential approach).
    /// </summary>
    public sealed class ThrusterLevel
    {
        public float Value { get; private set; }

        public void Reset(float value) => Value = value;

        /// <param name="riseRate">Per second, used while the target is above the current value.</param>
        /// <param name="fallRate">Per second, used while the target is below the current value.</param>
        public void Tick(float target, float riseRate, float fallRate, float deltaTime)
        {
            float rate = target > Value ? riseRate : fallRate;
            Value = target + (Value - target) * Mathf.Exp(-Mathf.Max(0f, rate) * deltaTime);
        }
    }
}
