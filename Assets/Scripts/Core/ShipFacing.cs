using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>Smoothed facing in [-1, 1] (+1 = screen right). A full flip takes <c>turnTime</c> seconds.</summary>
    public sealed class ShipFacing
    {
        const float InputDeadZone = 0.1f;

        public float Value { get; private set; } = 1f;

        public void Update(float horizontalInput, float turnTime, float dt)
        {
            if (Mathf.Abs(horizontalInput) < InputDeadZone) return;
            float rate = turnTime <= 0f ? float.MaxValue : 2f / turnTime;
            Value = Mathf.MoveTowards(Value, Mathf.Sign(horizontalInput), rate * dt);
        }
    }
}
