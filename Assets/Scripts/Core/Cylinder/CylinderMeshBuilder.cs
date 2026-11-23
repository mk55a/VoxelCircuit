using UnityEngine;

namespace CylinderShooter.Core
{
    /// <summary>
    /// Builds curved strips that follow the arena cylinder, using the same angle convention as CylinderSpace.
    /// UVs are measured in metres, so a texture keeps its size anywhere on the ring (Unity's cylinder primitive
    /// stretches one texture across the whole circumference). U grows toward screen-right (decreasing angle),
    /// so textures are not mirrored when viewed from outside the ring.
    /// </summary>
    public static class CylinderMeshBuilder
    {
        /// <summary>Number of straight segments needed so none is longer than <paramref name="maxSegmentLength"/>.</summary>
        public static int SegmentCount(float arcRadians, float radius, float maxSegmentLength)
        {
            float length = Mathf.Abs(arcRadians) * Mathf.Abs(radius);
            if (maxSegmentLength <= 0f) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(length / maxSegmentLength));
        }

        /// <summary>
        /// For a closed ring, adjusts the tile length so a whole number of tiles fits, hiding the texture seam
        /// where the ring meets itself. Open arcs keep the requested length.
        /// </summary>
        public static float FitTileLength(float arcLength, float tileLength, bool closed)
        {
            if (!closed || tileLength <= 0f || arcLength <= 0f) return tileLength;
            int tiles = Mathf.Max(1, Mathf.RoundToInt(arcLength / tileLength));
            return arcLength / tiles;
        }

        /// <summary>
        /// A vertical wall strip at <paramref name="radius"/> from <paramref name="bottom"/> to <paramref name="top"/>.
        /// U = metres along the arc / tile width. V = 0..1 bottom to top when <paramref name="tileHeight"/> is 0
        /// (one texture covers the full height), otherwise metres up the wall / tile height.
        /// </summary>
        public static CylinderMeshData Wall(float radius, float bottom, float top, float startAngle, float arcAngle,
            float tileWidth, float tileHeight, float maxSegmentLength, bool faceOutward = true)
        {
            int segments = SegmentCount(arcAngle, radius, maxSegmentLength);
            bool closed = Mathf.Abs(arcAngle) >= CylinderSpace.TwoPi - 1e-4f;
            float uScale = 1f / FitTileLength(Mathf.Abs(arcAngle) * radius, tileWidth, closed);
            float height = top - bottom;
            float vTop = tileHeight > 0f ? height / tileHeight : 1f;

            int count = (segments + 1) * 2;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var tangents = new Vector4[count];
            var uvs = new Vector2[count];

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + arcAngle * i / segments;
                Vector3 outward = CylinderSpace.Outward(angle);
                Vector3 normal = faceOutward ? outward : -outward;
                Vector3 uDirection = -CylinderSpace.Tangent(angle);           // screen-right
                float u = (angle - startAngle) * -radius * uScale;
                float w = Handedness(normal, uDirection, Vector3.up);

                int b = i * 2;
                vertices[b] = CylinderSpace.ToWorld(angle, bottom, radius);
                vertices[b + 1] = CylinderSpace.ToWorld(angle, top, radius);
                normals[b] = normals[b + 1] = normal;
                tangents[b] = tangents[b + 1] = new Vector4(uDirection.x, uDirection.y, uDirection.z, w);
                uvs[b] = new Vector2(u, 0f);
                uvs[b + 1] = new Vector2(u, vTop);
            }

            return new CylinderMeshData(vertices, normals, tangents, uvs,
                StripTriangles(segments, faceOutward == (arcAngle >= 0f)));
        }

        /// <summary>
        /// A flat horizontal ring (e.g. the floor) at <paramref name="height"/>, from <paramref name="innerRadius"/>
        /// out to <paramref name="outerRadius"/>. U = metres along the inner edge / tile width. V = 0..1 from the inner
        /// to the outer edge when <paramref name="tileDepth"/> is 0, otherwise metres outward / tile depth.
        /// </summary>
        public static CylinderMeshData Ring(float innerRadius, float outerRadius, float height, float startAngle,
            float arcAngle, float tileWidth, float tileDepth, float maxSegmentLength, bool faceUp = true)
        {
            int segments = SegmentCount(arcAngle, outerRadius, maxSegmentLength);
            bool closed = Mathf.Abs(arcAngle) >= CylinderSpace.TwoPi - 1e-4f;
            float uScale = 1f / FitTileLength(Mathf.Abs(arcAngle) * innerRadius, tileWidth, closed);
            float depth = outerRadius - innerRadius;
            float vOuter = tileDepth > 0f ? depth / tileDepth : 1f;
            Vector3 normal = faceUp ? Vector3.up : Vector3.down;

            int count = (segments + 1) * 2;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var tangents = new Vector4[count];
            var uvs = new Vector2[count];

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + arcAngle * i / segments;
                Vector3 uDirection = -CylinderSpace.Tangent(angle);
                float u = (angle - startAngle) * -innerRadius * uScale;
                float w = Handedness(normal, uDirection, CylinderSpace.Outward(angle));

                int b = i * 2;
                vertices[b] = CylinderSpace.ToWorld(angle, height, innerRadius);
                vertices[b + 1] = CylinderSpace.ToWorld(angle, height, outerRadius);
                normals[b] = normals[b + 1] = normal;
                tangents[b] = tangents[b + 1] = new Vector4(uDirection.x, uDirection.y, uDirection.z, w);
                uvs[b] = new Vector2(u, 0f);
                uvs[b + 1] = new Vector2(u, vOuter);
            }

            // Inner/outer vertex pairs wind the opposite way to a wall's bottom/top pairs, so the
            // up-facing ring uses the other winding (verified by the facing tests).
            return new CylinderMeshData(vertices, normals, tangents, uvs,
                StripTriangles(segments, faceUp != (arcAngle >= 0f)));
        }

        /// <summary>
        /// Tangent handedness (w) so that Unity's bitangent, cross(normal, tangent) * w, points along +V.
        /// </summary>
        private static float Handedness(Vector3 normal, Vector3 tangent, Vector3 vDirection) =>
            Vector3.Dot(Vector3.Cross(normal, tangent), vDirection) >= 0f ? 1f : -1f;

        /// <summary>
        /// Two triangles per segment over consecutive vertex pairs. The two windings face opposite ways;
        /// each caller picks the one whose front face matches its normal (Unity draws clockwise as the front).
        /// </summary>
        private static int[] StripTriangles(int segments, bool firstWinding)
        {
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int a0 = i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                int t = i * 6;
                if (firstWinding)
                {
                    triangles[t] = a0; triangles[t + 1] = b0; triangles[t + 2] = a1;
                    triangles[t + 3] = a1; triangles[t + 4] = b0; triangles[t + 5] = b1;
                }
                else
                {
                    triangles[t] = a0; triangles[t + 1] = a1; triangles[t + 2] = b0;
                    triangles[t + 3] = a1; triangles[t + 4] = b1; triangles[t + 5] = b0;
                }
            }
            return triangles;
        }
    }
}
