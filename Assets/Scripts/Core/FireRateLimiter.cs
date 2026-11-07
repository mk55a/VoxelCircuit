using System;

namespace CylinderShooter.Core
{
    /// <summary>Enforces a fixed fire rate against a caller-supplied clock, so it is testable.</summary>
    public sealed class FireRateLimiter
    {
        float _nextShotTime;

        public bool TryFire(float now, float shotsPerSecond)
        {
            if (shotsPerSecond <= 0f || now < _nextShotTime) return false;
            float interval = 1f / shotsPerSecond;
            // Keep cadence steady across frames, but don't bank shots after a long pause.
            _nextShotTime = now - _nextShotTime > interval ? now + interval : _nextShotTime + interval;
            return true;
        }

        public void Reset() => _nextShotTime = 0f;
    }
}
