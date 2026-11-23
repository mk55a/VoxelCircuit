using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class ShockwaveTests
    {
        private const float Radius = 60f;
        private const float Bottom = -6f;
        private const float Top = 6f;

        private sealed class Settings : IShockwaveSettings
        {
            // No build-up by default, so the travel tests start moving straight away.
            public float FormDuration { get; set; }
            public float FormRadius { get; set; } = 1f;
            public float FormDistance { get; set; } = 1f;
            public float Duration { get; set; } = 1f;
            public float Thickness { get; set; } = 4f;
            public float CurveDepth { get; set; } = 3f;
            public float FlattenDistance { get; set; }
            public float EaseOut { get; set; } = 1f;
        }

        private static Settings WithBuildUp() => new Settings
        {
            FormDuration = 0.5f, FormRadius = 1.2f, FormDistance = 4f, Duration = 1.4f,
            Thickness = 5f, CurveDepth = 3f, FlattenDistance = 14f, EaseOut = 2f
        };

        private static ShockwaveFront StartWave(Settings settings, float originAngle = 0f, float originHeight = 0f)
        {
            var front = new ShockwaveFront();
            front.Start(settings, originAngle, originHeight, Radius, Bottom, Top);
            return front;
        }

        /// <summary>Angle of a point this many metres along the surface from the origin (positive = screen-left).</summary>
        private static float AngleAt(float originAngle, float metres) => originAngle + metres / Radius;

        // ---------- Shape: o -> O -> ( ) ----------

        [Test]
        public void Starts_AsASmallRing_AroundThePlayer()
        {
            var front = StartWave(WithBuildUp(), 1f, 2f);
            Assert.IsFalse(front.IsReleased);
            Assert.AreEqual(2f, front.CenterHeight, 1e-4f, "centred on the player");
            Assert.AreEqual(1.2f, front.Distance, 1e-4f);
            Assert.AreEqual(1.2f, front.HalfHeight, 1e-4f);
            Assert.AreEqual(1.2f, front.CurveDepth, 1e-4f, "bend = radius: a circle");
        }

        [Test]
        public void WhileSmall_TheShapeIsACircle()
        {
            var front = StartWave(WithBuildUp());
            front.Advance(0.5f);
            float r = front.Distance;
            Assert.Less(r, front.FullHalfHeight, "still smaller than the cylinder");
            // On a circle of radius r, at height 0.6 r the front is at 0.8 r, i.e. it lags 0.2 r behind the middle.
            Assert.AreEqual(0.2f * r, front.LagAt(0.6f * r), 1e-3f);
            Assert.AreEqual(r, front.LagAt(r), 1e-3f, "the tips meet at the centre");
            Assert.IsTrue(float.IsPositiveInfinity(front.LagAt(r + 0.5f)), "above the ring");
        }

        [Test]
        public void OnceTallerThanTheCylinder_ItRelaxesIntoTheProfileBend()
        {
            var front = StartWave(WithBuildUp());
            front.Advance(0.5f);
            front.Advance(0.4f);
            Assert.Greater(front.Distance, front.FullHalfHeight + 14f, "past the flatten distance");
            Assert.AreEqual(front.FullHalfHeight, front.HalfHeight, 1e-4f, "covers the whole wall");
            Assert.AreEqual(3f, front.CurveDepth, 1e-4f);
            Assert.AreEqual(3f, front.LagAt(Top), 1e-3f);
            Assert.AreEqual(0f, front.LagAt(0f), 1e-5f);
        }

        [Test]
        public void Bend_StaysBetweenTheProfileDepthAndARoundRing_TheWholeWay()
        {
            var front = StartWave(WithBuildUp());
            for (int i = 0; i < 200 && front.IsActive; i++)
            {
                float round = Mathf.Min(front.Distance, front.FullHalfHeight);
                Assert.LessOrEqual(front.CurveDepth, round + 1e-4f, "never bent more than a circle");
                Assert.GreaterOrEqual(front.CurveDepth, Mathf.Min(3f, round) - 1e-4f, "never flatter than the profile's '( )'");
                Assert.LessOrEqual(front.LagAt(front.CenterHeight + front.HalfHeight), front.Distance + 1e-3f,
                    "the tips never pass the centre, so the halves never cross");
                front.Advance(0.01f);
            }
        }

        // ---------- Build-up ----------

        [Test]
        public void BuildUp_KillsAnEnemyTouchingTheShip_OnTheFirstFrame()
        {
            var front = StartWave(WithBuildUp(), 1f, 2f);
            Assert.IsTrue(front.Hits(AngleAt(1f, 0.8f), 2f, 0.5f), "no Advance yet: the ring already exists");
        }

        [Test]
        public void BuildUp_DoesNotReachTheTopOfTheCylinderYet()
        {
            var front = StartWave(WithBuildUp(), 1f, 2f);
            Assert.IsFalse(front.Hits(AngleAt(1f, 0.8f), Top, 0.3f));
        }

        [Test]
        public void BuildUp_MovesSlowly_ReleaseThrowsItFast()
        {
            var front = StartWave(WithBuildUp());
            front.Advance(0.25f);
            float formingSpeed = (front.Distance - front.PreviousDistance) / 0.25f;
            front.Advance(0.25f);
            Assert.IsTrue(front.IsReleased);
            Assert.AreEqual(4f, front.Distance, 1e-3f, "build-up ends at FormDistance");
            front.Advance(0.1f);
            float releaseSpeed = (front.Distance - front.PreviousDistance) / 0.1f;
            Assert.Greater(releaseSpeed, formingSpeed * 10f);
        }

        // ---------- Travel and hits ----------

        [Test]
        public void TargetAheadOfTheFront_IsNotHit()
        {
            var front = StartWave(new Settings());
            front.Advance(0.1f);
            Assert.Less(front.Distance, 50f);
            Assert.IsFalse(front.Hits(AngleAt(0f, 50f), 0f, 0.5f));
        }

        [Test]
        public void FrontSweepingOverATarget_HitsIt_EvenIfItJumpsPastInOneStep()
        {
            var front = StartWave(new Settings());
            front.Advance(0.1f);
            float before = front.Distance;
            front.Advance(0.1f);
            float midway = (before + front.Distance) * 0.5f;
            Assert.IsTrue(front.Hits(AngleAt(0f, midway), 0f, 0.1f));
        }

        [Test]
        public void TargetWellBehindTheBand_IsSafe()
        {
            // An enemy that appears where the wave has already passed must not die.
            var settings = new Settings();
            var front = StartWave(settings);
            for (int i = 0; i < 5; i++) front.Advance(0.1f);
            float behind = front.PreviousDistance - settings.Thickness - 5f;
            Assert.IsFalse(front.Hits(AngleAt(0f, behind), 0f, 0.5f));
        }

        [Test]
        public void BothDirections_AreHitAtTheSameDistance()
        {
            var front = StartWave(new Settings(), 1f);
            front.Advance(0.1f);
            float distance = front.Distance - 0.5f;
            Assert.IsTrue(front.Hits(AngleAt(1f, distance), 0f, 0.1f), "screen-left half");
            Assert.IsTrue(front.Hits(AngleAt(1f, -distance), 0f, 0.1f), "screen-right half");
        }

        [Test]
        public void Front_WrapsAcrossAngleZero()
        {
            var front = StartWave(new Settings(), 0.05f);
            front.Advance(0.1f);
            float target = CylinderSpace.WrapAngle(0.05f - (front.Distance - 0.5f) / Radius);
            Assert.Greater(target, Mathf.PI, "the target sits on the other side of angle 0");
            Assert.IsTrue(front.Hits(target, 0f, 0.1f));
        }

        [Test]
        public void LaggingTips_AreHitLaterThanTheMiddle()
        {
            var front = StartWave(new Settings());
            front.Advance(0.1f);
            Assert.IsTrue(front.Hits(AngleAt(0f, front.Distance - 1f), 0f, 0.1f), "the middle has reached it");
            Assert.IsFalse(front.Hits(AngleAt(0f, front.Distance - 0.5f), Top, 0.1f), "the lagging top has not");
        }

        [Test]
        public void Wave_ReachesTheFarSide_ThenEnds()
        {
            var front = StartWave(new Settings { Duration = 1f });
            front.Advance(0.6f);
            front.Advance(0.6f);
            Assert.AreEqual(1f, front.ReleaseProgress, 1e-5f);
            Assert.IsTrue(front.IsActive, "the step that reaches the far side still counts");
            Assert.GreaterOrEqual(front.Distance, Mathf.PI * Radius - 1e-3f);
            Assert.IsTrue(front.Hits(Mathf.PI, 0f, 0.5f), "the far side is hit");

            front.Advance(0.1f);
            Assert.IsFalse(front.IsActive);
            Assert.IsFalse(front.Hits(Mathf.PI, 0f, 0.5f));
        }

        [Test]
        public void Wave_EndsAfterBuildUpPlusTravel()
        {
            var front = StartWave(WithBuildUp());
            front.Advance(0.5f);
            front.Advance(1.4f);
            Assert.IsTrue(front.IsActive);
            Assert.AreEqual(1f, front.ReleaseProgress, 1e-4f);
            front.Advance(0.1f);
            Assert.IsFalse(front.IsActive);
        }

        [Test]
        public void EaseOut_StartsFasterThanConstantSpeed()
        {
            var linear = StartWave(new Settings { EaseOut = 1f });
            var eased = StartWave(new Settings { EaseOut = 2f });
            linear.Advance(0.2f);
            eased.Advance(0.2f);
            Assert.Greater(eased.Distance, linear.Distance);
        }

        [Test]
        public void FrontAngles_MoveApartSymmetrically()
        {
            var front = StartWave(new Settings(), 2f);
            front.Advance(0.25f);
            float right = CylinderSpace.ShortestDelta(2f, front.FrontAngle(ShockwaveFront.Right));
            float left = CylinderSpace.ShortestDelta(2f, front.FrontAngle(ShockwaveFront.Left));
            Assert.Less(right, 0f, "screen-right is decreasing angle");
            Assert.AreEqual(-right, left, 1e-4f);
        }
    }
}
