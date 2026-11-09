using System.Reflection;
using CylinderShooter.Core;
using CylinderShooter.Formations;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class FormationMotionTests
    {
        private FormationDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<FormationDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_definition);
        }

        [TestCase(FormationShape.HorizontalLine)]
        [TestCase(FormationShape.VerticalLine)]
        [TestCase(FormationShape.Grid)]
        public void Layout_UsesRequestedShape(FormationShape shape)
        {
            Set("_shape", shape);
            Set("_count", 3);
            Set("_columns", 2);
            Set("_rows", 2);
            var motion = new FormationMotion(_definition, new CylinderPosition(0f, 0f), 60f, 1);
            var first = motion.PositionAt(0);
            var next = motion.PositionAt(1);
            if (shape == FormationShape.VerticalLine)
                Assert.Greater(next.Height, first.Height);
            else
                Assert.Less(CylinderSpace.ShortestDelta(first.Angle, next.Angle), 0f);
            if (shape == FormationShape.Grid)
                Assert.Greater(motion.PositionAt(2).Height, first.Height);
        }

        [Test]
        public void Wave_MovesAndHasDifferentPhases()
        {
            Set("_shape", FormationShape.WaveQueue);
            var motion = new FormationMotion(_definition, new CylinderPosition(0f, 0f), 60f, 1);
            float first = motion.PositionAt(0).Height;
            Assert.AreNotEqual(first, motion.PositionAt(1).Height);
            motion.Advance(0.25f);
            Assert.AreNotEqual(first, motion.PositionAt(0).Height);
        }

        [Test]
        public void Validation_RejectsHeightEnvelopeAndCap()
        {
            Set("_shape", FormationShape.VerticalLine);
            Set("_count", 20);
            Assert.IsFalse(_definition.Validate(-6f, 6f, 150, out _));
            Assert.IsFalse(_definition.Validate(-100f, 100f, 10, out _));
        }

        [Test]
        public void Detection_UsesShortestArcAcrossSeamAndHeight()
        {
            Set("_detectionRange", 3f);
            var motion = new FormationMotion(_definition, new CylinderPosition(0f, 0f), 60f, 1);
            Assert.IsTrue(motion.IsWithinDetectionRange(new CylinderPosition(0.01f, 0f),
                new CylinderPosition(CylinderSpace.TwoPi - 0.01f, 2f)));
            Assert.IsFalse(motion.IsWithinDetectionRange(new CylinderPosition(0.01f, 0f),
                new CylinderPosition(CylinderSpace.TwoPi - 0.01f, 3f)));
        }

        private void Set(string field, object value)
        {
            typeof(FormationDefinition).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_definition, value);
        }
    }
}
