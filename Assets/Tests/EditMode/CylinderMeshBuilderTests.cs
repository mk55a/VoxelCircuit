using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class CylinderMeshBuilderTests
    {
        private const float Radius = 59.6f;
        private const float Start = -0.5f;
        private const float Arc = 1.0f;

        private static CylinderMeshData TestWall(bool outward = true, float arc = Arc) =>
            CylinderMeshBuilder.Wall(Radius, -7f, 8.5f, Start, arc, 16f, 0f, 1f, outward);

        private static CylinderMeshData TestRing(bool up = true, float arc = Arc) =>
            CylinderMeshBuilder.Ring(Radius, Radius + 6.4f, -7f, Start, arc, 16f, 0f, 1f, up);

        /// <summary>Unity draws clockwise triangles as the front face; this is that face's normal.</summary>
        private static Vector3 FrontNormal(CylinderMeshData data, int triangle)
        {
            var v = data.Vertices;
            int t = triangle * 3;
            Vector3 a = v[data.Triangles[t]], b = v[data.Triangles[t + 1]], c = v[data.Triangles[t + 2]];
            return Vector3.Cross(b - a, c - a);
        }

        private static Vector3 Centre(CylinderMeshData data, int triangle)
        {
            var v = data.Vertices;
            int t = triangle * 3;
            return (v[data.Triangles[t]] + v[data.Triangles[t + 1]] + v[data.Triangles[t + 2]]) / 3f;
        }

        [Test]
        public void SegmentCount_KeepsEverySegmentWithinTheLimit()
        {
            int segments = CylinderMeshBuilder.SegmentCount(Arc, Radius, 1f);
            Assert.AreEqual(Mathf.CeilToInt(Arc * Radius), segments);
            Assert.LessOrEqual(Arc * Radius / segments, 1f + 1e-4f);
            Assert.AreEqual(1, CylinderMeshBuilder.SegmentCount(0.001f, Radius, 1f));
        }

        [Test]
        public void Wall_VerticesSitOnTheRadius_AtTheRequestedHeights()
        {
            var wall = TestWall();
            for (int i = 0; i < wall.Vertices.Length; i++)
            {
                Vector3 p = wall.Vertices[i];
                Assert.AreEqual(Radius, new Vector2(p.x, p.z).magnitude, 1e-3f);
                Assert.AreEqual(i % 2 == 0 ? -7f : 8.5f, p.y, 1e-5f);
            }
        }

        [TestCase(Arc)]
        [TestCase(-Arc)]
        public void Wall_FacesOutward_TowardTheCamera(float arc)
        {
            var wall = TestWall(true, arc);
            for (int t = 0; t < wall.Triangles.Length / 3; t++)
            {
                Vector3 c = Centre(wall, t);
                Assert.Greater(Vector3.Dot(FrontNormal(wall, t), new Vector3(c.x, 0f, c.z)), 0f, $"triangle {t}");
            }
        }

        [Test]
        public void Wall_FacingInward_FlipsEveryTriangle()
        {
            var wall = TestWall(false);
            for (int t = 0; t < wall.Triangles.Length / 3; t++)
            {
                Vector3 c = Centre(wall, t);
                Assert.Less(Vector3.Dot(FrontNormal(wall, t), new Vector3(c.x, 0f, c.z)), 0f, $"triangle {t}");
            }
        }

        [TestCase(Arc)]
        [TestCase(-Arc)]
        public void Ring_FacesUp(float arc)
        {
            var ring = TestRing(true, arc);
            for (int t = 0; t < ring.Triangles.Length / 3; t++)
                Assert.Greater(FrontNormal(ring, t).y, 0f, $"triangle {t}");
        }

        [Test]
        public void Ring_VerticesSpanTheInnerAndOuterRadius()
        {
            var ring = TestRing();
            for (int i = 0; i < ring.Vertices.Length; i++)
            {
                Vector3 p = ring.Vertices[i];
                Assert.AreEqual(i % 2 == 0 ? Radius : Radius + 6.4f, new Vector2(p.x, p.z).magnitude, 1e-3f);
                Assert.AreEqual(-7f, p.y, 1e-5f);
            }
        }

        [Test]
        public void Uv_U_GrowsTowardScreenRight_SoTexturesAreNotMirrored()
        {
            // Screen-right is the direction of decreasing angle, and later vertices have larger angles.
            var wall = TestWall();
            Assert.Greater(wall.Uvs[0].x, wall.Uvs[2].x);
        }

        [Test]
        public void Uv_U_IsMeasuredInMetresOfArc()
        {
            var wall = TestWall();
            float firstToLast = wall.Uvs[0].x - wall.Uvs[wall.Uvs.Length - 2].x;
            Assert.AreEqual(Arc * Radius / 16f, firstToLast, 1e-3f);
        }

        [Test]
        public void Uv_V_SpansZeroToOne_WhenTileHeightIsZero()
        {
            var wall = TestWall();
            Assert.AreEqual(0f, wall.Uvs[0].y, 1e-6f);
            Assert.AreEqual(1f, wall.Uvs[1].y, 1e-6f);
        }

        [Test]
        public void Tangents_BitangentPointsAlongV()
        {
            var wall = TestWall();
            var ring = TestRing();
            for (int i = 0; i < wall.Vertices.Length; i += 2)
            {
                Vector4 t = wall.Tangents[i];
                Vector3 bitangent = Vector3.Cross(wall.Normals[i], new Vector3(t.x, t.y, t.z)) * t.w;
                Assert.Greater(Vector3.Dot(bitangent, Vector3.up), 0.99f, "wall: +V is up");

                t = ring.Tangents[i];
                bitangent = Vector3.Cross(ring.Normals[i], new Vector3(t.x, t.y, t.z)) * t.w;
                Vector3 p = ring.Vertices[i];
                Assert.Greater(Vector3.Dot(bitangent, new Vector3(p.x, 0f, p.z).normalized), 0.99f, "ring: +V is outward");
            }
        }

        [Test]
        public void FitTileLength_FullRing_UsesAWholeNumberOfTiles()
        {
            float circumference = CylinderSpace.TwoPi * Radius;
            float tile = CylinderMeshBuilder.FitTileLength(circumference, 16f, true);
            float tiles = circumference / tile;
            Assert.AreEqual(Mathf.Round(tiles), tiles, 1e-3f);
            Assert.AreEqual(16f, tile, 1f);
        }

        [Test]
        public void FitTileLength_OpenArc_KeepsTheRequestedLength()
        {
            Assert.AreEqual(16f, CylinderMeshBuilder.FitTileLength(50f, 16f, false));
        }

        [Test]
        public void Triangles_TwoPerSegment()
        {
            var wall = TestWall();
            int segments = wall.Vertices.Length / 2 - 1;
            Assert.AreEqual(segments * 6, wall.Triangles.Length);
        }
    }
}
