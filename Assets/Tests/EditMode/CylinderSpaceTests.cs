using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class CylinderSpaceTests
    {
        const float Eps = 1e-4f;

        [TestCase(0f, 0f)]
        [TestCase(-0.5f, CylinderSpace.TwoPi - 0.5f)]
        [TestCase(CylinderSpace.TwoPi + 0.25f, 0.25f)]
        [TestCase(-CylinderSpace.TwoPi * 3 + 0.1f, 0.1f)]
        public void WrapAngle_ReturnsValueInRange(float input, float expected)
        {
            Assert.AreEqual(expected, CylinderSpace.WrapAngle(input), Eps);
        }

        [Test]
        public void WrapAngle_NeverReturnsTwoPi()
        {
            Assert.Less(CylinderSpace.WrapAngle(-1e-9f), CylinderSpace.TwoPi);
        }

        [Test]
        public void ShortestDelta_CrossesSeamTheShortWay()
        {
            float from = CylinderSpace.TwoPi - 0.1f;
            Assert.AreEqual(0.2f, CylinderSpace.ShortestDelta(from, 0.1f), Eps);
            Assert.AreEqual(-0.2f, CylinderSpace.ShortestDelta(0.1f, from), Eps);
        }

        [Test]
        public void ShortestDelta_IsWithinHalfTurn()
        {
            for (float a = 0; a < CylinderSpace.TwoPi; a += 0.37f)
            for (float b = 0; b < CylinderSpace.TwoPi; b += 0.41f)
                Assert.LessOrEqual(Mathf.Abs(CylinderSpace.ShortestDelta(a, b)), Mathf.PI + Eps);
        }

        [Test]
        public void ToWorld_MatchesDesignFormula()
        {
            var p = CylinderSpace.ToWorld(Mathf.PI / 2f, 3f, 10f);
            Assert.AreEqual(10f, p.x, Eps);
            Assert.AreEqual(3f, p.y, Eps);
            Assert.AreEqual(0f, p.z, Eps);
        }

        [Test]
        public void FullLoop_ReturnsToStartWithoutJump()
        {
            var start = new CylinderPosition(1f, 2f);
            var pos = start;
            Vector3 prev = pos.ToWorld(10f);
            const int steps = 1000;
            for (int i = 0; i < steps; i++)
            {
                pos = pos.Moved(CylinderSpace.TwoPi / steps, 0f);
                var world = pos.ToWorld(10f);
                Assert.Less(Vector3.Distance(prev, world), 0.1f, "jump at step " + i);
                prev = world;
            }
            Assert.AreEqual(0f, Vector3.Distance(start.ToWorld(10f), pos.ToWorld(10f)), 1e-3f);
        }

        [Test]
        public void Lerp_InterpolatesAcrossSeam()
        {
            var a = new CylinderPosition(CylinderSpace.TwoPi - 0.1f, 0f);
            var b = new CylinderPosition(0.1f, 0f);
            var mid = CylinderPosition.Lerp(a, b, 0.5f);
            Assert.AreEqual(0f, CylinderSpace.ShortestDelta(0f, mid.Angle), Eps);
        }
    }
}
