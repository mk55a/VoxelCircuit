using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Turns engine state into one "thrust level": 0 = idle, 1 = cruising at top speed, 2 = full boost.
    /// Above cruise speed the level follows the speed, so the exhaust shrinks back as boost momentum bleeds off.
    /// </summary>
    public static class ThrusterMath
    {
        public const float Idle = 0f;
        public const float Cruise = 1f;
        public const float Boost = 2f;

        public static float TargetLevel(float speed, float cruiseSpeed, float boostSpeed, bool boosting)
        {
            if (boosting) return Boost;
            if (cruiseSpeed <= 0f) return Idle;

            float magnitude = Mathf.Abs(speed);
            float cruise = Mathf.Clamp01(magnitude / cruiseSpeed);
            float overdrive = boostSpeed > cruiseSpeed
                ? Mathf.Clamp01((magnitude - cruiseSpeed) / (boostSpeed - cruiseSpeed))
                : 0f;
            return cruise + overdrive;
        }
    }
}
