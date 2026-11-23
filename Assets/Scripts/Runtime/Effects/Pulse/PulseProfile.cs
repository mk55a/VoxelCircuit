using UnityEngine;

namespace CylinderShooter.Effects
{
    /// <summary>
    /// How one kind of pulse looks ("Enemy Vaporize", "Elite Kill"). Sizes are multiples of the destroyed
    /// model's diameter, so one profile fits enemies of any size. Brightness comes from the materials.
    /// </summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Effects/Pulse Profile")]
    public sealed class PulseProfile : ScriptableObject
    {
        [Header("Colour")]
        [Tooltip("Tint for every layer. Use the player's bullet colour so kills read as 'my shot did that'.")]
        [SerializeField] private Color _color = new Color(0.49f, 0.98f, 1f);

        [Header("Flash")]
        [SerializeField, Min(0f)] private float _flashSize = 1f;
        [SerializeField, Min(0.01f)] private float _flashLifetime = 0.06f;

        [Header("Ring")]
        [SerializeField, Min(0f)] private float _ringSize = 1.5f;
        [SerializeField, Min(0.01f)] private float _ringLifetime = 0.22f;

        [Header("Sparks")]
        [SerializeField, Min(0)] private int _sparkCount = 10;
        [Tooltip("m/s, random between x and y.")]
        [SerializeField] private Vector2 _sparkSpeed = new Vector2(8f, 16f);
        [SerializeField] private Vector2 _sparkLifetime = new Vector2(0.15f, 0.3f);
        [Tooltip("Spark thickness in metres (length comes from speed on a Stretched Billboard).")]
        [SerializeField, Min(0.01f)] private float _sparkSize = 0.08f;

        public Color Color => _color;
        public float FlashSize => _flashSize;
        public float FlashLifetime => _flashLifetime;
        public float RingSize => _ringSize;
        public float RingLifetime => _ringLifetime;
        public int SparkCount => _sparkCount;
        public Vector2 SparkSpeed => _sparkSpeed;
        public Vector2 SparkLifetime => _sparkLifetime;
        public float SparkSize => _sparkSize;
    }
}
