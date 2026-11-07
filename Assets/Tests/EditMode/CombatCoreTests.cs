using CylinderShooter.Core;
using NUnit.Framework;
using UnityEngine;

namespace CylinderShooter.Tests
{
    public class CombatCoreTests
    {
        [Test]
        public void Pool_PrewarmsAndReusesInstances()
        {
            int created = 0;
            var pool = new ObjectPool<object>(() => { created++; return new object(); }, prewarm: 3);
            Assert.AreEqual(3, created);
            Assert.AreEqual(3, pool.CountInactive);

            var a = pool.Get();
            pool.Release(a);
            Assert.AreSame(a, pool.Get());
            Assert.AreEqual(3, created);
        }

        [Test]
        public void Pool_GrowsWhenEmptyAndInvokesCallbacks()
        {
            int gets = 0, releases = 0;
            var pool = new ObjectPool<object>(() => new object(), _ => gets++, _ => releases++);
            var a = pool.Get();
            var b = pool.Get();
            Assert.AreEqual(2, pool.CountAll);
            Assert.AreEqual(2, pool.CountActive);
            pool.Release(a);
            pool.Release(b);
            Assert.AreEqual(2, gets);
            Assert.AreEqual(2, releases);
        }

        [Test]
        public void FireRate_AllowsConfiguredShotsPerSecond()
        {
            var limiter = new FireRateLimiter();
            int shots = 0;
            for (int i = 0; i < 600; i++)
                if (limiter.TryFire(i / 60f, 10f)) shots++;
            Assert.That(shots, Is.InRange(99, 101));
        }

        [Test]
        public void FireRate_DoesNotBankShotsAfterPause()
        {
            var limiter = new FireRateLimiter();
            Assert.IsTrue(limiter.TryFire(0f, 10f));
            Assert.IsTrue(limiter.TryFire(100f, 10f));
            Assert.IsFalse(limiter.TryFire(100.01f, 10f));
        }

        [Test]
        public void Overlap_DetectsTouchingAndSeparatedSpheres()
        {
            Assert.IsTrue(Overlap.Spheres(Vector3.zero, 1f, new Vector3(1.9f, 0, 0), 1f));
            Assert.IsFalse(Overlap.Spheres(Vector3.zero, 1f, new Vector3(2.1f, 0, 0), 1f));
        }

        interface IFoo { }
        sealed class Foo : IFoo { }

        [Test]
        public void ServiceLocator_ResolvesRegisteredAndThrowsOtherwise()
        {
            ServiceLocator.Clear();
            Assert.Throws<System.InvalidOperationException>(() => ServiceLocator.Get<IFoo>());
            var foo = new Foo();
            ServiceLocator.Register<IFoo>(foo);
            Assert.AreSame(foo, ServiceLocator.Get<IFoo>());
            ServiceLocator.Unregister<IFoo>(foo);
            Assert.IsFalse(ServiceLocator.TryGet<IFoo>(out _));
        }
    }
}
