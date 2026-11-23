using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Combat
{
    /// <summary>Tuning for one kind of shockwave: build-up, travel, what it kills and how it is drawn.</summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Combat/Shockwave Profile")]
    public sealed class ShockwaveProfile : ScriptableObject, IShockwaveSettings
    {
        [Header("Build-Up")]
        [Tooltip("Seconds the wave spends as a slowly growing ring around the ship before it is thrown. It already kills while forming.")]
        [SerializeField, Min(0f)] private float _formDuration = 0.5f;
        [Tooltip("Radius of the starting ring ('o'), in metres. About half the ship's length.")]
        [SerializeField, Min(0.1f)] private float _formRadius = 1.2f;
        [Tooltip("Radius of the ring at the end of the build-up, in metres.")]
        [SerializeField, Min(0f)] private float _formDistance = 4f;

        [Header("Travel")]
        [Tooltip("Seconds after the build-up for the two halves to meet on the far side of the cylinder.")]
        [SerializeField, Min(0.05f)] private float _duration = 1.4f;
        [Tooltip("1 = constant speed. Higher = thrown out of the ring fast, then slows down.")]
        [SerializeField, Min(1f)] private float _easeOut = 2.2f;

        [Header("Kill Band")]
        [Tooltip("Width of the band behind the front that destroys enemies, in metres.")]
        [SerializeField, Min(0.1f)] private float _thickness = 5f;
        [Tooltip("Damage dealt to every target the band touches. Large = always kills.")]
        [SerializeField, Min(1)] private int _damage = 1000;

        [Header("Shape")]
        [Tooltip("How far the top and bottom lag behind the middle once the ring has relaxed into '( )', in metres.")]
        [SerializeField, Min(0f)] private float _curveDepth = 3f;
        [Tooltip("Metres of travel, after the ring is taller than the cylinder, over which the round 'O' relaxes into '( )'.")]
        [SerializeField, Min(0f)] private float _flattenDistance = 14f;

        [Header("Visual")]
        [Tooltip("Brightness of the wave over time, in seconds since K was pressed. Peaks at the release.")]
        [SerializeField] private AnimationCurve _glowOverTime = new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.5f, 3f), new Keyframe(0.75f, 1f), new Keyframe(2f, 1f));
        [Tooltip("Longest the glowing trail behind the front can be, in metres. It is always kept shorter than the " +
                 "ring's radius, so the two halves never cross in the middle.")]
        [SerializeField, Min(0.1f)] private float _visualLength = 10f;
        [Tooltip("Extra height above and below the play area, so the formed wave fills the whole cylinder wall.")]
        [SerializeField, Min(0f)] private float _heightPadding = 1f;
        [Tooltip("Pushes the wave slightly towards the camera so it draws over enemies on the surface.")]
        [SerializeField] private float _radiusOffset = 0.6f;
        [Tooltip("Fraction of the travel time at the end over which the wave fades out.")]
        [SerializeField, Range(0f, 1f)] private float _fadeOut = 0.25f;

        public float FormDuration => _formDuration;
        public float FormRadius => _formRadius;
        public float FormDistance => _formDistance;
        public float Duration => _duration;
        public float EaseOut => _easeOut;
        public float Thickness => _thickness;
        public int Damage => _damage;
        public float CurveDepth => _curveDepth;
        public float FlattenDistance => _flattenDistance;
        public float VisualLength => _visualLength;
        public float HeightPadding => _heightPadding;
        public float RadiusOffset => _radiusOffset;
        public float FadeOut => _fadeOut;

        public float GlowAt(float secondsSinceTrigger) => Mathf.Max(0f, _glowOverTime.Evaluate(secondsSinceTrigger));
    }
}
