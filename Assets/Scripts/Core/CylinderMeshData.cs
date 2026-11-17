using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>Plain mesh arrays produced by CylinderMeshBuilder; a Unity component copies them into a Mesh.</summary>
    public sealed class CylinderMeshData
    {
        public CylinderMeshData(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, int[] triangles)
        {
            Vertices = vertices;
            Normals = normals;
            Tangents = tangents;
            Uvs = uvs;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Vector4[] Tangents { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; }
    }
}
