using System;
using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>How an exhaust looks at one thrust level. Profiles store three (idle, cruise, boost) and blend between them.</summary>
    [Serializable]
    public struct ExhaustState
    {
        [Tooltip("Flame length in metres.")]
        [SerializeField, Min(0f)] private float _length;
        [Tooltip("Multiplier on the nozzle size set by the flame's X/Y scale.")]
        [SerializeField, Min(0f)] private float _width;
        [Tooltip("Flame shader intensity. Above about 1.3 the flame starts to bloom.")]
        [SerializeField, Min(0f)] private float _brightness;
        [SerializeField, Min(0f)] private float _lightIntensity;

        public ExhaustState(float length, float width, float brightness, float lightIntensity)
        {
            _length = length;
            _width = width;
            _brightness = brightness;
            _lightIntensity = lightIntensity;
        }

        public float Length => _length;
        public float Width => _width;
        public float Brightness => _brightness;
        public float LightIntensity => _lightIntensity;

        public static ExhaustState Lerp(ExhaustState from, ExhaustState to, float t)
        {
            t = Mathf.Clamp01(t);
            return new ExhaustState(
                Mathf.Lerp(from._length, to._length, t),
                Mathf.Lerp(from._width, to._width, t),
                Mathf.Lerp(from._brightness, to._brightness, t),
                Mathf.Lerp(from._lightIntensity, to._lightIntensity, t));
        }

        /// <summary>Blends idle -> cruise over levels 0..1 and cruise -> boost over levels 1..2.</summary>
        public static ExhaustState Evaluate(ExhaustState idle, ExhaustState cruise, ExhaustState boost, float level)
        {
            return level <= ThrusterMath.Cruise
                ? Lerp(idle, cruise, level)
                : Lerp(cruise, boost, level - ThrusterMath.Cruise);
        }
    }
}
