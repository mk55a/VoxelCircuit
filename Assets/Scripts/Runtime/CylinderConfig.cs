using UnityEngine;

namespace CylinderShooter
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Cylinder Config")]
    public sealed class CylinderConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float _radius = 20f;
        [SerializeField] private float _minHeight = -6f;
        [SerializeField] private float _maxHeight = 6f;

        public float Radius => _radius;
        public float MinHeight => _minHeight;
        public float MaxHeight => _maxHeight;
    }
}
