#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;

namespace CylinderShooter.Debugging
{
    public sealed class FpsSampler
    {
        private const double SampleInterval = 0.5;

        private double _elapsed;
        private int _frames;

        public bool RecordFrame(float unscaledDeltaTime, out float fps)
        {
            if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime) || unscaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));

            fps = 0f;
            if (unscaledDeltaTime == 0f) return false;

            _elapsed += unscaledDeltaTime;
            _frames++;
            if (_elapsed < SampleInterval) return false;

            fps = (float)(_frames / _elapsed);
            _elapsed = 0d;
            _frames = 0;
            return true;
        }
    }
}
#endif
