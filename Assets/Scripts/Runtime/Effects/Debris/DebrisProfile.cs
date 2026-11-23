using UnityEngine;

namespace CylinderShooter.Effects
{
    public enum DebrisColorMode
    {
        /// <summary>Each piece keeps the colour of the cube it came from.</summary>
        ModelColors,
        /// <summary>Every piece uses one colour (e.g. metallic grey).</summary>
        Uniform
    }

    /// <summary>
    /// How one kind of destruction looks and moves. One asset per feel ("Enemy", "Player Death", "Metal Scrap")
    /// shared by any number of destructibles, tuned in the Inspector without code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Effects/Debris Profile")]
    public sealed class DebrisProfile : ScriptableObject
    {
        [Header("Colour")]
        [SerializeField] private DebrisColorMode _colorMode = DebrisColorMode.ModelColors;
        [SerializeField] private Color _uniformColor = new Color(0.42f, 0.44f, 0.5f);

        [Header("Pieces")]
        [Tooltip("Large models emit every Nth cube (scaled up) so a single burst never exceeds this.")]
        [SerializeField, Min(1)] private int _maxPieces = 120;
        [SerializeField, Min(0.1f)] private float _sizeMultiplier = 1f;

        [Header("Blast (m/s, random between x and y)")]
        [Tooltip("Outward from the model's centre.")]
        [SerializeField] private Vector2 _burstSpeed = new Vector2(2f, 6f);
        [Tooltip("Along the push direction (away from the instigator).")]
        [SerializeField] private Vector2 _pushSpeed = new Vector2(4f, 9f);
        [Tooltip("Straight up: a small hop before falling.")]
        [SerializeField] private Vector2 _upSpeed = new Vector2(1f, 4f);
        [Tooltip("Toward the camera, so pieces don't fly into the arena and vanish.")]
        [SerializeField] private Vector2 _towardCameraSpeed = new Vector2(0.5f, 2.5f);
        [SerializeField, Min(0f)] private float _maxSpinDegreesPerSecond = 540f;

        [Header("Life")]
        [SerializeField] private Vector2 _lifetime = new Vector2(2.5f, 4f);

        public DebrisColorMode ColorMode => _colorMode;
        public Color UniformColor => _uniformColor;
        public int MaxPieces => _maxPieces;
        public float SizeMultiplier => _sizeMultiplier;
        public Vector2 BurstSpeed => _burstSpeed;
        public Vector2 PushSpeed => _pushSpeed;
        public Vector2 UpSpeed => _upSpeed;
        public Vector2 TowardCameraSpeed => _towardCameraSpeed;
        public float MaxSpinDegreesPerSecond => _maxSpinDegreesPerSecond;
        public Vector2 Lifetime => _lifetime;
    }
}
