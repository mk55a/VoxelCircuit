using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class ShipTests
    {
        sealed class Tuning : IShipTuning
        {
            public float Thrust => 60f;
            public float VerticalThrust => 40f;
            public float Drag => 3f;
            public float MaxSpeed => 25f;
            public float MaxVerticalSpeed => 12f;
            public float ReverseThrustMultiplier => 2f;
            public float TurnTime => 0.15f;
            public float BoostSpeedMultiplier => 1.8f;
            public float BoostThrustMultiplier => 2f;
            public float BoostDuration => 0.5f;
            public float BoostCooldown => 2f;
            public float BoostRecoveryDeceleration => 30f;
        }

        const float Dt = 1f / 60f;

        [Test]
        public void Thrust_NeverExceedsMaxSpeed()
        {
            var motor = new ShipMotor(new Tuning());
            for (int i = 0; i < 600; i++) motor.Step(Vector2.right, false, Dt);
            Assert.LessOrEqual(motor.Velocity.x, 25f + 1e-3f);
            Assert.Greater(motor.Velocity.x, 5f);
        }

        [Test]
        public void Drag_SlowsShipWithoutInput()
        {
            var motor = new ShipMotor(new Tuning());
            for (int i = 0; i < 120; i++) motor.Step(Vector2.right, false, Dt);
            float before = motor.Velocity.x;
            for (int i = 0; i < 120; i++) motor.Step(Vector2.zero, false, Dt);
            Assert.Less(motor.Velocity.x, before * 0.1f);
        }

        [Test]
        public void Boost_RaisesTopSpeed()
        {
            var motor = new ShipMotor(new Tuning());
            for (int i = 0; i < 600; i++) motor.Step(Vector2.right, true, Dt);
            Assert.Greater(motor.Velocity.x, 25f);
        }

        [TestCase(1f, 1f)]
        [TestCase(-1f, -1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 0f)]
        [TestCase(1f, -1f)]
        [TestCase(-1f, 1f)]
        public void BoostEnd_RetainsMomentumAndReturnsToNormalSpeed(float direction, float input)
        {
            var motor = new ShipMotor(new Tuning());
            for (int i = 0; i < 600; i++) motor.Step(Vector2.right * direction, true, Dt);
            float boostedSpeed = Mathf.Abs(motor.Velocity.x);

            motor.Step(Vector2.right * input, false, Dt);
            float firstSpeed = Mathf.Abs(motor.Velocity.x);
            Assert.Greater(firstSpeed, 25f, "Boost speed should not snap to the normal limit.");
            Assert.Less(firstSpeed, boostedSpeed);
            Assert.Less(boostedSpeed - firstSpeed, 5f);

            for (int i = 0; i < 120; i++) motor.Step(Vector2.right * input, false, Dt);
            Assert.LessOrEqual(Mathf.Abs(motor.Velocity.x), 25f + 1e-3f);
        }

        [Test]
        public void Facing_FlipsInTurnTime()
        {
            var facing = new ShipFacing();
            for (int i = 0; i < 9; i++) facing.Update(-1f, 0.15f, Dt);
            Assert.AreEqual(-1f, facing.Value, 1e-4f);
        }

        [Test]
        public void Boost_RespectsDurationAndCooldown()
        {
            var boost = new BoostAbility(new Tuning());
            Assert.IsTrue(boost.TryActivate());
            Assert.IsFalse(boost.TryActivate());
            for (int i = 0; i < 31; i++) boost.Tick(Dt);
            Assert.IsFalse(boost.IsActive);
            Assert.IsFalse(boost.IsReady);
            for (int i = 0; i < 125; i++) boost.Tick(Dt);
            Assert.IsTrue(boost.IsReady);
        }

        [Test]
        public void Boost_InterruptStartsFullCooldownAndPreservesExistingCooldown()
        {
            var boost = new BoostAbility(new Tuning());
            Assert.IsTrue(boost.TryActivate());
            boost.Tick(0.1f);
            boost.Interrupt();
            Assert.IsFalse(boost.IsActive);
            Assert.AreEqual(2f, boost.CooldownRemaining, 0.0001f);
            boost.Tick(0.5f);
            boost.Interrupt();
            Assert.AreEqual(1.5f, boost.CooldownRemaining, 0.0001f);
        }
    }
}
