using CylinderShooter.Core;
using CylinderShooter.Effects;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class DebrisTests
    {
        [Test]
        public void AwayAlongSurface_SourceAtLargerAngle_PointsTowardIncreasingAngle()
        {
            Vector3 away = DebrisMath.AwayAlongSurface(0f, 0.1f);
            Assert.Greater(Vector3.Dot(away, CylinderSpace.Tangent(0.1f)), 0.99f);
        }

        [Test]
        public void AwayAlongSurface_SourceAtSmallerAngle_PointsTowardDecreasingAngle()
        {
            Vector3 away = DebrisMath.AwayAlongSurface(0.1f, 0f);
            Assert.Less(Vector3.Dot(away, CylinderSpace.Tangent(0f)), -0.99f);
        }

        [Test]
        public void AwayAlongSurface_AcrossTheWrap_UsesTheShortWayRound()
        {
            // Player just past 0, enemy just before 2π: the enemy is "behind" (smaller angle) by 0.1 rad.
            float source = CylinderSpace.TwoPi - 0.05f;
            Vector3 away = DebrisMath.AwayAlongSurface(0.05f, source);
            Assert.Less(Vector3.Dot(away, CylinderSpace.Tangent(source)), -0.99f);
        }

        [TestCase(422, 120, 4)]
        [TestCase(100, 120, 1)]
        [TestCase(120, 120, 1)]
        [TestCase(121, 120, 2)]
        public void Stride_CapsThePieceCount(int voxels, int max, int expected)
        {
            int stride = DebrisMath.Stride(voxels, max);
            Assert.AreEqual(expected, stride);
            Assert.LessOrEqual(Mathf.CeilToInt(voxels / (float)stride), max);
        }

        [Test]
        public void VoxelShape_Parse_ReadsPositionsAndColours()
        {
            const string json = "{\"name\":\"T\",\"voxelSize\":0.1," +
                "\"palette\":[{\"name\":\"a\",\"hex\":\"#FF0000\",\"emissive\":false}," +
                "{\"name\":\"b\",\"hex\":\"#00FF00\",\"emissive\":true}]," +
                "\"voxels\":[{\"p\":[0.05,0.15,-0.25],\"c\":1}]}";
            var shape = VoxelShape.Parse(json);

            Assert.AreEqual(1, shape.Count);
            Assert.AreEqual(0.1f, shape.VoxelSize, 1e-6f);
            Assert.Less(Vector3.Distance(new Vector3(0.05f, 0.15f, -0.25f), shape.Positions[0]), 1e-5f);
            Assert.AreEqual(new Color32(0, 255, 0, 255), shape.Colors[0]);
        }

        [Test]
        public void VoxelShape_Parse_RejectsBadColourIndex()
        {
            const string json = "{\"name\":\"T\",\"voxelSize\":0.1," +
                "\"palette\":[{\"name\":\"a\",\"hex\":\"#FF0000\",\"emissive\":false}]," +
                "\"voxels\":[{\"p\":[0,0,0],\"c\":3}]}";
            Assert.Throws<System.FormatException>(() => VoxelShape.Parse(json));
        }
    }
}
