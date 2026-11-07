using UnityEngine;

namespace CylinderShooter
{
    [CreateAssetMenu(menuName = "Cylinder Shooter/Cylinder Config")]
    public sealed class CylinderConfig : ScriptableObject
    {
        [Min(1f)] public float radius = 20f;
        public float minHeight = -6f;
        public float maxHeight = 6f;
    }
}
