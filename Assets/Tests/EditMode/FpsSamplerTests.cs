using System;
using CylinderShooter.Debugging;
using NUnit.Framework;

namespace CylinderShooter.Tests
{
    public class FpsSamplerTests
    {
        [Test]
        public void RecordFrame_WaitsForHalfSecondAndUsesActualElapsedTime()
        {
            var sampler = new FpsSampler();
            Assert.IsFalse(sampler.RecordFrame(0.2f, out _));
            Assert.IsFalse(sampler.RecordFrame(0.2f, out _));
            Assert.IsTrue(sampler.RecordFrame(0.2f, out float fps));
            Assert.That(fps, Is.EqualTo(5f).Within(0.0001f));

            Assert.IsFalse(sampler.RecordFrame(0.25f, out _));
            Assert.IsTrue(sampler.RecordFrame(0.25f, out fps));
            Assert.That(fps, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void RecordFrame_ZeroDurationIsNotCounted()
        {
            var sampler = new FpsSampler();
            Assert.IsFalse(sampler.RecordFrame(0f, out _));
            Assert.IsTrue(sampler.RecordFrame(0.5f, out float fps));
            Assert.That(fps, Is.EqualTo(2f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void RecordFrame_InvalidDurationThrows(float duration)
        {
            var sampler = new FpsSampler();
            Assert.Throws<ArgumentOutOfRangeException>(() => sampler.RecordFrame(duration, out _));
        }
    }
}
