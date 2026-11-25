using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public sealed class ProximityFuseTests
    {
        private sealed class Settings : IFuseSettings
        {
            public float TriggerRange { get; set; } = 4f;
            public float FuseDuration { get; set; } = 0.6f;
            public float BlinkRateStart { get; set; } = 5f;
            public float BlinkRateEnd { get; set; } = 15f;
        }

        [Test]
        public void StaysIdle_WhilePlayerOutsideRange()
        {
            var fuse = new ProximityFuse(new Settings());
            Assert.IsFalse(fuse.Tick(1f, 4.01f, true));
            Assert.AreEqual(FuseState.Idle, fuse.State);
            Assert.AreEqual(0f, fuse.BlinkLevel);
        }

        [Test]
        public void ArmsAtTriggerRange_AndLampStartsLit()
        {
            var fuse = new ProximityFuse(new Settings());
            Assert.IsFalse(fuse.Tick(0f, 4f, true));
            Assert.IsTrue(fuse.IsArmed);
            Assert.AreEqual(1f, fuse.BlinkLevel);
        }

        [Test]
        public void DoesNotArm_WhilePlayerIsRespawning()
        {
            var fuse = new ProximityFuse(new Settings());
            fuse.Tick(0.1f, 1f, false);
            Assert.AreEqual(FuseState.Idle, fuse.State);
        }

        [Test]
        public void Detonates_ExactlyOnceAfterFuseDuration()
        {
            var fuse = new ProximityFuse(new Settings());
            Assert.IsFalse(fuse.Tick(0.3f, 1f, true));
            Assert.IsFalse(fuse.Tick(0.29f, 1f, true));
            Assert.IsTrue(fuse.Tick(0.02f, 1f, true));
            Assert.AreEqual(FuseState.Detonated, fuse.State);
            Assert.IsFalse(fuse.Tick(1f, 1f, true));
        }

        [Test]
        public void OnceArmed_KeepsCountingWhenPlayerLeaves()
        {
            var fuse = new ProximityFuse(new Settings());
            fuse.Tick(0.1f, 1f, true);
            Assert.IsFalse(fuse.Tick(0.3f, 100f, true));
            Assert.IsTrue(fuse.Tick(0.3f, 100f, false));
        }

        [Test]
        public void Progress_RunsFromZeroToOne()
        {
            var fuse = new ProximityFuse(new Settings());
            fuse.Tick(0f, 1f, true);
            Assert.AreEqual(0f, fuse.Progress, 1e-5f);
            fuse.Tick(0.3f, 1f, true);
            Assert.AreEqual(0.5f, fuse.Progress, 1e-4f);
        }

        [Test]
        public void Blinks_FasterNearTheEnd()
        {
            var settings = new Settings { FuseDuration = 10f, BlinkRateStart = 2f, BlinkRateEnd = 20f };
            int early = CountBlinkChanges(new ProximityFuse(settings), 0f, 1f);
            var late = new ProximityFuse(settings);
            int lateChanges = CountBlinkChanges(late, 8.9f, 1f);
            Assert.Greater(lateChanges, early);
        }

        [Test]
        public void Reset_ReturnsToIdle()
        {
            var fuse = new ProximityFuse(new Settings());
            fuse.Tick(1f, 1f, true);
            fuse.Reset();
            Assert.AreEqual(FuseState.Idle, fuse.State);
            Assert.AreEqual(0f, fuse.Progress);
        }

        [Test]
        public void MarkDetonated_StopsAnArmedFuse()
        {
            var fuse = new ProximityFuse(new Settings());
            fuse.Tick(0.1f, 1f, true);
            fuse.MarkDetonated();
            Assert.IsFalse(fuse.Tick(1f, 1f, true));
            Assert.IsFalse(fuse.IsArmed);
        }

        [Test]
        public void SurfaceDistance_MeasuresAcrossTheSeam()
        {
            const float radius = 60f;
            var a = new CylinderPosition(0.01f, 0f);
            var b = new CylinderPosition(CylinderSpace.TwoPi - 0.01f, 0f);
            Assert.AreEqual(0.02f * radius, CylinderSpace.SurfaceDistance(a, b, radius), 1e-3f);
            var c = new CylinderPosition(0.01f, 3f);
            Assert.AreEqual(3f, CylinderSpace.SurfaceDistance(a, c, radius), 1e-4f);
            Assert.AreEqual(5f, CylinderSpace.SurfaceDistance(new CylinderPosition(0f, 0f),
                new CylinderPosition(4f / radius, 3f), radius), 1e-3f);
        }

        /// <summary>Arms the fuse, skips <paramref name="skip"/> seconds, then counts lamp on/off changes over a window.</summary>
        private static int CountBlinkChanges(ProximityFuse fuse, float skip, float window)
        {
            fuse.Tick(0f, 1f, true);
            for (float t = 0f; t < skip; t += 0.01f) fuse.Tick(0.01f, 1f, true);
            int changes = 0;
            float last = fuse.BlinkLevel;
            for (float t = 0f; t < window; t += 0.005f)
            {
                fuse.Tick(0.005f, 1f, true);
                if (!Mathf.Approximately(fuse.BlinkLevel, last)) changes++;
                last = fuse.BlinkLevel;
            }
            return changes;
        }
    }
}
