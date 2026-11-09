using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class PulseTests
    {
        [Test]
        public void BoundingRadius_UnitCubeAtOrigin_IsHalfTheDiagonal()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            Assert.AreEqual(Mathf.Sqrt(3f) * 0.5f, PulseMath.BoundingRadius(bounds, Matrix4x4.identity), 1e-5f);
        }

        [Test]
        public void BoundingRadius_ScalesWithTheTransform()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            float radius = PulseMath.BoundingRadius(bounds, Matrix4x4.Scale(Vector3.one * 2f));
            Assert.AreEqual(Mathf.Sqrt(3f), radius, 1e-5f);
        }

        [Test]
        public void BoundingRadius_OffCentreBounds_ReachTheFarCorner()
        {
            // A 1 m cube sitting 2 m in front of the origin: the far corner is at (0.5, 0.5, 2.5).
            var bounds = new Bounds(new Vector3(0f, 0f, 2f), Vector3.one);
            float expected = new Vector3(0.5f, 0.5f, 2.5f).magnitude;
            Assert.AreEqual(expected, PulseMath.BoundingRadius(bounds, Matrix4x4.identity), 1e-5f);
        }

        [Test]
        public void BoundingRadius_IgnoresRotation_ForACube()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            var rotated = Matrix4x4.Rotate(Quaternion.Euler(30f, 45f, 60f));
            Assert.AreEqual(Mathf.Sqrt(3f) * 0.5f, PulseMath.BoundingRadius(bounds, rotated), 1e-5f);
        }

        [TestCase(0f)]
        [TestCase(1.3f)]
        [TestCase(4f)]
        public void ScreenPlaneDirection_IsUnitLength_AndNeverPointsAtTheCamera(float surfaceAngle)
        {
            for (int i = 0; i < 16; i++)
            {
                Vector3 direction = PulseMath.ScreenPlaneDirection(surfaceAngle, i * Mathf.PI / 8f);
                Assert.AreEqual(1f, direction.magnitude, 1e-5f);
                Assert.AreEqual(0f, Vector3.Dot(direction, CylinderSpace.Outward(surfaceAngle)), 1e-5f);
            }
        }
    }
}
