using CylinderShooter.Core;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    public class ExhaustTests
    {
        private const float CruiseSpeed = 22f;
        private const float BoostSpeed = 39.6f;

        [Test]
        public void TargetLevel_WhileBoosting_IsBoost()
        {
            Assert.AreEqual(ThrusterMath.Boost, ThrusterMath.TargetLevel(0f, CruiseSpeed, BoostSpeed, true));
        }

        [Test]
        public void TargetLevel_Stopped_IsIdle()
        {
            Assert.AreEqual(ThrusterMath.Idle, ThrusterMath.TargetLevel(0f, CruiseSpeed, BoostSpeed, false));
        }

        [Test]
        public void TargetLevel_AtCruiseSpeed_IsCruise()
        {
            Assert.AreEqual(ThrusterMath.Cruise, ThrusterMath.TargetLevel(CruiseSpeed, CruiseSpeed, BoostSpeed, false), 1e-5f);
        }

        [Test]
        public void TargetLevel_AfterBoost_FollowsTheLeftoverSpeed()
        {
            // Halfway between cruise and boost speed: the exhaust should be halfway between cruise and boost.
            float speed = (CruiseSpeed + BoostSpeed) * 0.5f;
            Assert.AreEqual(1.5f, ThrusterMath.TargetLevel(speed, CruiseSpeed, BoostSpeed, false), 1e-5f);
        }

        [Test]
        public void TargetLevel_IgnoresDirection()
        {
            Assert.AreEqual(
                ThrusterMath.TargetLevel(15f, CruiseSpeed, BoostSpeed, false),
                ThrusterMath.TargetLevel(-15f, CruiseSpeed, BoostSpeed, false));
        }

        [Test]
        public void TargetLevel_NeverExceedsBoost()
        {
            Assert.AreEqual(ThrusterMath.Boost, ThrusterMath.TargetLevel(BoostSpeed * 3f, CruiseSpeed, BoostSpeed, false), 1e-5f);
        }

        [Test]
        public void Level_RisesFasterThanItFalls()
        {
            var rising = new ThrusterLevel();
            rising.Reset(0f);
            rising.Tick(2f, 14f, 3.5f, 0.1f);

            var falling = new ThrusterLevel();
            falling.Reset(2f);
            falling.Tick(0f, 14f, 3.5f, 0.1f);

            Assert.Greater(rising.Value, 2f - falling.Value);
        }

        [Test]
        public void Level_IsFrameRateIndependent()
        {
            var oneStep = new ThrusterLevel();
            oneStep.Tick(2f, 10f, 10f, 0.2f);

            var manySteps = new ThrusterLevel();
            for (int i = 0; i < 20; i++) manySteps.Tick(2f, 10f, 10f, 0.01f);

            Assert.AreEqual(oneStep.Value, manySteps.Value, 1e-4f);
        }

        [Test]
        public void State_BlendsIdleCruiseAndBoostByLevel()
        {
            var idle = new ExhaustState(0f, 1f, 1f, 0f);
            var cruise = new ExhaustState(1f, 1f, 2f, 1f);
            var boost = new ExhaustState(3f, 2f, 4f, 3f);

            Assert.AreEqual(0f, ExhaustState.Evaluate(idle, cruise, boost, 0f).Length, 1e-5f);
            Assert.AreEqual(1f, ExhaustState.Evaluate(idle, cruise, boost, 1f).Length, 1e-5f);
            Assert.AreEqual(2f, ExhaustState.Evaluate(idle, cruise, boost, 1.5f).Length, 1e-5f);
            Assert.AreEqual(3f, ExhaustState.Evaluate(idle, cruise, boost, 1.5f).Brightness, 1e-5f);
            Assert.AreEqual(3f, ExhaustState.Evaluate(idle, cruise, boost, 2f).Length, 1e-5f);
        }
    }
}
