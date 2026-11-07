using CylinderShooter.Core;
using CylinderShooter.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class ChaserSteeringTests
    {
        [Test]
        public void TurnTowards_LimitsRotationPerStep()
        {
            var position = new CylinderPosition(0f, 0f);
            var target = new CylinderPosition(0f, 10f);
            float heading = ChaserSteering.TurnTowards(0f, position, target, 20f, 60f, 0.1f);
            Assert.AreEqual(6f, heading, 0.001f);
        }

        [TestCase(0.01f, -0.01f, 0f)]
        [TestCase(-0.01f, 0.01f, 180f)]
        public void TurnTowards_FollowsShortestPathAcrossSeam(float start, float target, float expected)
        {
            float heading = ChaserSteering.TurnTowards(expected, new CylinderPosition(start, 0f),
                new CylinderPosition(target, 0f), 20f, 180f, 1f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(expected, heading)), Is.LessThan(0.001f));
        }

        [Test]
        public void TurnTowards_AtTargetKeepsHeading()
        {
            var position = new CylinderPosition(1f, 2f);
            Assert.AreEqual(45f, ChaserSteering.TurnTowards(45f, position, position, 20f, 90f, 1f));
        }

        [Test]
        public void Move_AdvancesAtConfiguredSpeed()
        {
            var position = new CylinderPosition(0f, 2f);
            var result = ChaserSteering.Move(position, 30f, 10f, 20f, 0.5f);
            Assert.AreEqual(2.5f, result.Height - position.Height, 0.001f);
            Assert.AreEqual(-Mathf.Cos(30f * Mathf.Deg2Rad) * 5f / 20f,
                CylinderSpace.ShortestDelta(position.Angle, result.Angle), 0.001f);
        }

        [TestCase(1f, 1f, 0.02f)]
        [TestCase(1f, -1f, 0.02f)]
        [TestCase(-1f, 1f, 0.02f)]
        [TestCase(-1f, -1f, 0.02f)]
        [TestCase(1f, 1f, 0.05f)]
        [TestCase(-1f, -1f, 0.05f)]
        public void BoundaryPursuit_TurnsBeforeEdgeWithoutLosingSpeed(
            float verticalDirection, float horizontalDirection, float deltaTime)
        {
            const float radius = 20f;
            const float speed = 7f;
            const float turnSpeed = 100f;
            var position = new CylinderPosition(0f, 0f);
            float heading = horizontalDirection > 0f ? 45f * verticalDirection : 180f - 45f * verticalDirection;
            float maximumHeight = 0f;
            for (int i = 0; i < 300; i++)
            {
                var target = new CylinderPosition(
                    position.Angle - horizontalDirection * 0.1f, verticalDirection * 6f);
                float nextHeading = ChaserSteering.TurnWithinBounds(heading, position, target, radius,
                    speed, turnSpeed, deltaTime, -6f, 6f);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(heading, nextHeading)),
                    turnSpeed * deltaTime + 0.001f);
                var unrestricted = ChaserSteering.Move(position, nextHeading, speed, radius, deltaTime);
                Assert.That(unrestricted.Height, Is.InRange(-6f, 6f), "Unexpected safety fallback");
                var next = ChaserSteering.MoveWithinBounds(position, nextHeading, speed, radius, deltaTime, -6f, 6f);
                float arc = CylinderSpace.ShortestDelta(position.Angle, next.Angle) * radius;
                float distance = new Vector2(arc, next.Height - position.Height).magnitude;
                Assert.AreEqual(speed * deltaTime, distance, 0.001f);
                maximumHeight = Mathf.Max(maximumHeight, verticalDirection * next.Height);
                position = next;
                heading = nextHeading;
            }
            Assert.Greater(maximumHeight, 4f, "Chaser should be able to approach the boundary.");
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void BoundaryFallback_KeepsHeightAndDoesNotReflectHeading(float direction)
        {
            var position = new CylinderPosition(0f, direction * 6f);
            var target = new CylinderPosition(-0.2f, direction * 6f);
            float heading = direction * 90f;
            float nextHeading = ChaserSteering.TurnWithinBounds(heading, position, target,
                20f, 7f, 100f, 0.02f, -6f, 6f);
            var next = ChaserSteering.MoveWithinBounds(position, nextHeading, 7f, 20f, 0.02f, -6f, 6f);
            Assert.AreEqual(direction * 6f, next.Height);
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(heading, nextHeading)), 2.001f);
            Assert.Greater(Mathf.Sin(nextHeading * Mathf.Deg2Rad) * direction, 0f);
        }

        [Test]
        public void ZeroTurnRate_StillPreventsLeavingArena()
        {
            var position = new CylinderPosition(0f, 6f);
            var target = new CylinderPosition(1f, 0f);
            float heading = ChaserSteering.TurnWithinBounds(90f, position, target, 20f, 7f, 0f, 0.5f, -6f, 6f);
            var next = ChaserSteering.MoveWithinBounds(position, heading, 7f, 20f, 0.5f, -6f, 6f);
            Assert.AreEqual(90f, heading, 0.001f);
            Assert.AreEqual(6f, next.Height);
        }

        [TestCase(6f, 0f, -0.5f)]
        [TestCase(-6f, 180f, 0.5f)]
        public void AtBoundary_CanTravelAlongEdge(float height, float heading, float targetAngle)
        {
            var position = new CylinderPosition(0f, height);
            var target = new CylinderPosition(targetAngle, height);
            float nextHeading = ChaserSteering.TurnWithinBounds(heading, position, target, 20f, 7f, 100f, 0.02f, -6f, 6f);
            var next = ChaserSteering.MoveWithinBounds(position, nextHeading, 7f, 20f, 0.02f, -6f, 6f);
            Assert.AreEqual(height, next.Height, 0.00001f);
            Assert.AreEqual(0.14f, Mathf.Abs(CylinderSpace.ShortestDelta(position.Angle, next.Angle) * 20f), 0.001f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(heading, nextHeading), 0.001f);
        }

        [TestCase(6f, -1f)]
        [TestCase(-6f, 1f)]
        public void AtBoundary_TurnsBackTowardInterior(float height, float verticalDirection)
        {
            var position = new CylinderPosition(0f, height);
            var target = new CylinderPosition(-0.2f, 0f);
            float heading = ChaserSteering.TurnWithinBounds(0f, position, target, 20f, 7f, 100f, 0.02f, -6f, 6f);
            Assert.Greater(Mathf.Sin(heading * Mathf.Deg2Rad) * verticalDirection, 0f);
        }

        [Test]
        public void BoundarySteering_FollowsMovingTargetAcrossSeam()
        {
            const float radius = 20f;
            const float deltaTime = 0.02f;
            var position = new CylinderPosition(0.02f, 4f);
            float heading = 0f;
            for (int i = 0; i < 500; i++)
            {
                var target = new CylinderPosition(-0.05f - i * 0.002f, 5.5f * Mathf.Sin(i * 0.01f));
                float nextHeading = ChaserSteering.TurnWithinBounds(heading, position, target,
                    radius, 7f, 100f, deltaTime, -6f, 6f);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(heading, nextHeading)), 2.001f);
                var unrestricted = ChaserSteering.Move(position, nextHeading, 7f, radius, deltaTime);
                Assert.That(unrestricted.Height, Is.InRange(-6f, 6f));
                position = unrestricted;
                heading = nextHeading;
            }
        }

        [Test]
        public void Prefab_HasTuningAndVisibleMesh()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chaser.prefab");
            Assert.IsNotNull(prefab);
            var chaser = prefab.GetComponent<Chaser>();
            Assert.IsNotNull(chaser);
            Assert.IsInstanceOf<ChaserTuning>(new SerializedObject(chaser)
                .FindProperty("_tuning").objectReferenceValue);
            Assert.IsNotNull(prefab.GetComponent<MeshRenderer>());
            Assert.IsNotNull(prefab.GetComponent<MeshFilter>().sharedMesh);
        }
    }
}
