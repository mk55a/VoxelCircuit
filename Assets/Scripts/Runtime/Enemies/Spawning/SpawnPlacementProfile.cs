using UnityEngine;

namespace CylinderShooter.Enemies
{
    /// <summary>Where the spawner may put a group so it arrives out of the player's sight, and apart from other groups.</summary>
    [CreateAssetMenu(menuName = "Cylinder Shooter/Director/Spawn Placement")]
    public sealed class SpawnPlacementProfile : ScriptableObject
    {
        [Tooltip("Extra margin outside the screen edge, as a fraction of the screen width, so nothing pops in at the edge.")]
        [SerializeField, Min(0f)] private float _viewportMargin = 0.02f;
        [Tooltip("Points at least this far from the camera, in metres, count as hidden (lost in the fog behind the glass " +
                 "arena). 0 = only off-screen points count.")]
        [SerializeField, Min(0f)] private float _hiddenBeyondDistance = 102f;
        [Tooltip("How many places around half the cylinder are tried when looking for a hidden spot. More = finer, slower.")]
        [SerializeField, Range(16, 1024)] private int _searchSteps = 256;
        [Tooltip("Metres a new group's centre keeps from groups already in the arena at a similar height, so groups " +
                 "don't arrive on top of each other.")]
        [SerializeField, Min(0f)] private float _groupSpacing = 12f;

        public float ViewportMargin => _viewportMargin;
        public float HiddenBeyondDistance => _hiddenBeyondDistance;
        public int SearchSteps => _searchSteps;
        public float GroupSpacing => _groupSpacing;
    }
}
