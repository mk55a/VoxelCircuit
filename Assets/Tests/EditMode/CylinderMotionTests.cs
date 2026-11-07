using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class CylinderMotionTests
    {
        [TestCase(1f)]
        [TestCase(-1f)]
        public void MoveHorizontally_WrapsSmoothlyInBothDirections(float direction)
        {
            const float radius = 20f;
            const float step = 0.02f;
            const float speed = 4f;
            var start = new CylinderPosition(direction > 0f ? 0.001f : CylinderSpace.TwoPi - 0.001f, 3f);
            var end = CylinderMotion.MoveHorizontally(start, direction * speed, radius, step);

            Assert.AreEqual(-direction * speed * step / radius,
                CylinderSpace.ShortestDelta(start.Angle, end.Angle), 0.00001f);
            Assert.AreEqual(3f, end.Height);
            Assert.That(Vector3.Distance(start.ToWorld(radius), end.ToWorld(radius)), Is.EqualTo(speed * step).Within(0.001f));
        }

        [Test]
        public void MoveHorizontally_ReturnsToStartAfterOneLap()
        {
            const float radius = 20f;
            const float speed = 4f;
            var start = new CylinderPosition(1f, 2f);
            var end = CylinderMotion.MoveHorizontally(start, speed, radius, CylinderSpace.TwoPi * radius / speed);

            Assert.That(Vector3.Distance(start.ToWorld(radius), end.ToWorld(radius)), Is.LessThan(0.001f));
        }
    }
}
