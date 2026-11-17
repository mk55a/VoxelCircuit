using CylinderShooter.Core;
using UnityEngine;

namespace CylinderShooter.Arena
{
    public enum CylinderBandShape
    {
        /// <summary>A vertical strip of the arena wall.</summary>
        Wall,
        /// <summary>A flat ring, e.g. the floor below the play band.</summary>
        Ring
    }

    /// <summary>
    /// Generates a curved arena piece that follows the cylinder: a wall strip or a floor ring, over any arc.
    /// Edit the fields and the mesh rebuilds immediately, in edit mode and in play mode, so a small section can be
    /// tuned first and later widened to the full 360 degrees. The mesh is generated, not saved as an asset.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CylinderBand : MonoBehaviour
    {
        [SerializeField] private CylinderConfig _cylinder;
        [SerializeField] private CylinderBandShape _shape = CylinderBandShape.Wall;

        [Header("Placement")]
        [Tooltip("Distance from the play surface: negative sits just behind the ships (inside the ring). " +
                 "For a Ring this is the inner edge.")]
        [SerializeField] private float _radiusOffset = -0.4f;
        [Tooltip("Ring only: how far the ring extends outward (toward the camera), in metres.")]
        [SerializeField, Min(0.01f)] private float _ringWidth = 6.4f;
        [Tooltip("Wall: bottom edge. Ring: its height.")]
        [SerializeField] private float _bottom = -7f;
        [Tooltip("Wall only: top edge.")]
        [SerializeField] private float _top = 8.5f;
        [Tooltip("Where the arc starts, in degrees (same angles as the ship's start angle).")]
        [SerializeField] private float _startAngleDegrees = -30f;
        [SerializeField, Range(1f, 360f)] private float _arcDegrees = 60f;

        [Header("Surface")]
        [Tooltip("Metres of arc covered by one texture repeat.")]
        [SerializeField, Min(0.1f)] private float _tileWidth = 16f;
        [Tooltip("0 = one texture covers the full height (wall) or width (ring). Otherwise metres per repeat.")]
        [SerializeField, Min(0f)] private float _tileHeight;
        [Tooltip("Longest straight segment, in metres. Smaller is smoother; 1 m is invisible at radius 60.")]
        [SerializeField, Min(0.05f)] private float _maxSegmentLength = 1f;

        private Mesh _mesh;
        private bool _dirty = true;

        public CylinderConfig Cylinder => _cylinder;

        private void OnEnable()
        {
            _dirty = true;
            Rebuild();
        }

        private void OnValidate() => _dirty = true;     // rebuild on the next Update, not inside OnValidate

        private void Update()
        {
            if (_dirty) Rebuild();
        }

        private void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }

        public void Rebuild()
        {
            _dirty = false;
            if (_cylinder == null)
            {
                Debug.LogError("CylinderBand requires a cylinder config.", this);
                return;
            }

            float radius = _cylinder.Radius + _radiusOffset;
            float start = _startAngleDegrees * Mathf.Deg2Rad;
            float arc = _arcDegrees * Mathf.Deg2Rad;
            var data = _shape == CylinderBandShape.Wall
                ? CylinderMeshBuilder.Wall(radius, _bottom, _top, start, arc, _tileWidth, _tileHeight, _maxSegmentLength)
                : CylinderMeshBuilder.Ring(radius, radius + _ringWidth, _bottom, start, arc, _tileWidth, _tileHeight,
                    _maxSegmentLength);

            if (_mesh == null)
            {
                _mesh = new Mesh { name = $"{name} (generated)", hideFlags = HideFlags.DontSave };
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }
            _mesh.Clear();
            _mesh.SetVertices(data.Vertices);
            _mesh.SetNormals(data.Normals);
            _mesh.SetTangents(data.Tangents);
            _mesh.SetUVs(0, data.Uvs);
            _mesh.SetTriangles(data.Triangles, 0);
            _mesh.RecalculateBounds();
        }
    }
}
