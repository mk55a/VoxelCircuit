using UnityEngine;

namespace CylinderShooter.City
{
    public enum CityLayout
    {
        GridStreets,
        NoiseClusters
    }

    [DisallowMultipleComponent]
    public sealed class CityGenerator : MonoBehaviour
    {
        [Tooltip("The plane whose surface supports the city. Generated buildings move with it.")]
        [SerializeField] private MeshFilter _plane;
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private GameObject[] _buildingPrefabs;
        [SerializeField, Min(0)] private int _minimumBuildings = 15;
        [SerializeField, Min(0)] private int _maximumBuildings = 25;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private CityLayout _layout = CityLayout.GridStreets;
        [Tooltip("Keep buildings this far inside the arena radius, in world units.")]
        [SerializeField, Min(0f)] private float _boundaryMargin = 2f;
        [Tooltip("Minimum gap between building footprints, in world units.")]
        [SerializeField, Min(0f)] private float _clearance = 1f;
        [Tooltip("Spacing between candidate building centers, in world units.")]
        [SerializeField, Min(0.1f)] private float _cellSpacing = 12f;
        [Tooltip("Grid mode: number of building rows or columns between streets.")]
        [SerializeField, Min(1)] private int _blockSize = 3;
        [Tooltip("Grid mode: extra space reserved for each street, in world units.")]
        [SerializeField, Min(0f)] private float _streetWidth = 6f;
        [Tooltip("Cluster mode: size of noise features, in world units.")]
        [SerializeField, Min(0.1f)] private float _noiseScale = 35f;
        [Tooltip("Cluster mode: candidates below this noise value are excluded.")]
        [SerializeField, Range(0f, 1f)] private float _noiseThreshold = 0.4f;
        [SerializeField, HideInInspector] private GameObject _generatedRoot;

        public MeshFilter Plane => _plane;
        public CylinderConfig Cylinder => _cylinder;
        public GameObject[] BuildingPrefabs => _buildingPrefabs;
        public int MinimumBuildings => _minimumBuildings;
        public int MaximumBuildings => _maximumBuildings;
        public int Seed => _seed;
        public CityLayout Layout => _layout;
        public float BoundaryMargin => _boundaryMargin;
        public float Clearance => _clearance;
        public float CellSpacing => _cellSpacing;
        public int BlockSize => _blockSize;
        public float StreetWidth => _streetWidth;
        public float NoiseScale => _noiseScale;
        public float NoiseThreshold => _noiseThreshold;
        public GameObject GeneratedRoot => _generatedRoot;
    }
}
