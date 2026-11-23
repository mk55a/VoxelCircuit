using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// How one kind of engine exhaust behaves ("Kestrel Exhaust", later enemy engines).
    /// Colours live in the flame material; this asset holds size, brightness and response.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Effects/Exhaust Profile")]
    public sealed class ExhaustProfile : ScriptableObject
    {
        [Header("States")]
        [SerializeField] private ExhaustState _idle = new ExhaustState(0.35f, 0.8f, 1.1f, 0.4f);
        [SerializeField] private ExhaustState _cruise = new ExhaustState(0.9f, 1f, 1.8f, 1.2f);
        [SerializeField] private ExhaustState _boost = new ExhaustState(2.6f, 1.35f, 3.2f, 3.5f);

        [Header("Response")]
        [Tooltip("How fast the exhaust grows towards a higher level, per second. Higher = snappier flare on boost.")]
        [SerializeField, Min(0f)] private float _riseRate = 14f;
        [Tooltip("How fast it shrinks back, per second. Lower = a longer, calmer settle after boost.")]
        [SerializeField, Min(0f)] private float _fallRate = 3.5f;

        public float RiseRate => _riseRate;
        public float FallRate => _fallRate;

        /// <param name="level">0 = idle, 1 = cruise, 2 = boost (see <see cref="ThrusterMath"/>).</param>
        public ExhaustState Evaluate(float level) => ExhaustState.Evaluate(_idle, _cruise, _boost, level);
    }
}
