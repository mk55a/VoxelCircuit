using System.Collections;
using System.Reflection;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public class DrifterTests
    {
        private GameObject _object;
        private GameObject _bulletObject;
        private CylinderConfig _cylinder;
        private DrifterTuning _tuning;
        private TargetRegistry _registry;
        private ContactHazardRegistry _hazards;

        [SetUp]
        public void SetUp()
        {
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            _tuning = ScriptableObject.CreateInstance<DrifterTuning>();
            _registry = new TargetRegistry();
            _hazards = new ContactHazardRegistry();
            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(_registry);
            ServiceLocator.Register(_hazards);
            _object = new GameObject("Test Drifter");
            _object.SetActive(false);
            var drifter = _object.AddComponent<Drifter>();
            typeof(Drifter).GetField("_tuning", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(drifter, _tuning);
            _object.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
            if (_bulletObject != null)
            {
                Object.DestroyImmediate(_bulletObject);
            }
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_registry);
            ServiceLocator.Unregister(_hazards);
            Object.DestroyImmediate(_cylinder);
            Object.DestroyImmediate(_tuning);
        }

        [UnityTest]
        public IEnumerator Drifter_MovesAndDiesInOneHit()
        {
            var drifter = _object.GetComponent<Drifter>();
            Assert.IsTrue(drifter.IsAlive);
            Assert.AreEqual(1, _registry.Targets.Count);
            Assert.AreSame(drifter, _registry.Targets[0]);
            Vector3 start = drifter.Position;
            yield return null;
            yield return null;
            Assert.Greater(Vector3.Distance(start, drifter.Position), 0f);
            Assert.That(drifter.Position.y, Is.EqualTo(start.y).Within(0.001f));

            drifter.TakeHit(1);
            Assert.IsFalse(drifter.IsAlive);
            Assert.IsFalse(_object.activeSelf);
            Assert.IsEmpty(_registry.Targets);

            _object.SetActive(true);
            Assert.IsTrue(drifter.IsAlive);
            Assert.AreEqual(1, _registry.Targets.Count);
        }

        [Test]
        public void Drifter_IgnoresNonPositiveDamage()
        {
            var drifter = _object.GetComponent<Drifter>();
            drifter.TakeHit(0);
            drifter.TakeHit(-1);
            Assert.IsTrue(drifter.IsAlive);
            Assert.AreEqual(1, _registry.Targets.Count);
        }

        [UnityTest]
        public IEnumerator Bullet_HitsAndReleasesDrifter()
        {
            var drifter = _object.GetComponent<Drifter>();
            _bulletObject = new GameObject("Test Bullet");
            var bullet = _bulletObject.AddComponent<Bullet>();
            bool released = false;
            bullet.Initialize(_cylinder.radius, _registry, _ => released = true);
            bullet.Launch(new CylinderPosition(0f, 0f), Vector2.zero, 1f, 0.4f, 1);

            yield return null;
            yield return null;

            Assert.IsFalse(drifter.IsAlive);
            Assert.IsTrue(released);
            Assert.IsEmpty(_registry.Targets);
        }
    }
}
