using System.Collections;
using System.Reflection;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public sealed class ChaserTestInput : MonoBehaviour, IShipInput
    {
        public Vector2 Move => Vector2.zero;
        public bool ConsumeBoost() => false;
    }

    public class ChaserTests
    {
        private GameObject _chaserObject;
        private GameObject _targetObject;
        private GameObject _bulletObject;
        private CylinderConfig _cylinder;
        private ChaserTuning _tuning;
        private ShipTuning _shipTuning;
        private TargetRegistry _registry;
        private ContactHazardRegistry _hazards;
        private Chaser _chaser;
        private ShipController _ship;

        [SetUp]
        public void SetUp()
        {
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            _tuning = ScriptableObject.CreateInstance<ChaserTuning>();
            _shipTuning = ScriptableObject.CreateInstance<ShipTuning>();
            _registry = new TargetRegistry();
            _hazards = new ContactHazardRegistry();
            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(_registry);
            ServiceLocator.Register(_hazards);

            _targetObject = new GameObject("Test target");
            _targetObject.SetActive(false);
            var input = _targetObject.AddComponent<ChaserTestInput>();
            _ship = _targetObject.AddComponent<ShipController>();
            SetField(_ship, "_cylinder", _cylinder);
            SetField(_ship, "_tuning", _shipTuning);
            SetField(_ship, "_inputSource", input);
            SetField(_ship, "_startAngleDegrees", -0.5f * Mathf.Rad2Deg);
            _targetObject.SetActive(true);
            _ship.enabled = false;
            _chaserObject = new GameObject("Test chaser");
            _chaserObject.SetActive(false);
            _chaser = _chaserObject.AddComponent<Chaser>();
            SetField("_tuning", _tuning);
            SetField("_target", _ship);
            _chaserObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_chaserObject);
            Object.DestroyImmediate(_targetObject);
            if (_bulletObject != null)
            {
                Object.DestroyImmediate(_bulletObject);
            }
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_registry);
            ServiceLocator.Unregister(_hazards);
            Object.DestroyImmediate(_cylinder);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_shipTuning);
        }

        [UnityTest]
        public IEnumerator Chaser_PursuesTargetAndResetsWhenReenabled()
        {
            float startDistance = Vector3.Distance(_chaser.Position,
                CylinderSpace.ToWorld(-0.5f, 0f, _cylinder.Radius));
            yield return null;
            yield return null;
            Assert.Less(Vector3.Distance(_chaser.Position,
                CylinderSpace.ToWorld(-0.5f, 0f, _cylinder.Radius)), startDistance);
            Assert.AreEqual(1, _registry.Targets.Count);

            _chaser.TakeHit(1);
            Assert.IsTrue(_chaser.IsAlive);
            _chaser.TakeHit(1);
            Assert.IsFalse(_chaser.IsAlive);
            Assert.IsEmpty(_registry.Targets);
            _chaserObject.SetActive(true);
            Assert.IsTrue(_chaser.IsAlive);
            Assert.AreEqual(1, _registry.Targets.Count);
            _chaser.TakeHit(1);
            Assert.IsTrue(_chaser.IsAlive);
        }

        [UnityTest]
        public IEnumerator Bullet_DamagesAndRemovesChaser()
        {
            _chaser.TakeHit(1);
            _bulletObject = new GameObject("Test bullet");
            var bullet = _bulletObject.AddComponent<Bullet>();
            bool released = false;
            bullet.Initialize(_cylinder.Radius, _registry, _ => released = true);
            bullet.Launch(new CylinderPosition(0f, 0f), Vector2.zero, 1f, 0.4f, 1);
            yield return null;
            yield return null;
            Assert.IsFalse(_chaser.IsAlive);
            Assert.IsTrue(released);
            Assert.IsEmpty(_registry.Targets);
        }

        [UnityTest]
        public IEnumerator Chaser_AtUpperEdgeContinuesWithoutBouncing()
        {
            return CheckEdgeWithoutBounce(1f);
        }

        [UnityTest]
        public IEnumerator Chaser_AtLowerEdgeContinuesWithoutBouncing()
        {
            return CheckEdgeWithoutBounce(-1f);
        }

        private IEnumerator CheckEdgeWithoutBounce(float direction)
        {
            _chaserObject.SetActive(false);
            float edge = direction > 0f ? _cylinder.MaxHeight : _cylinder.MinHeight;
            SetField("_startHeight", edge);
            SetField("_startHeadingDegrees", direction * 90f);
            var position = new CylinderPosition(-0.5f, edge);
            SetField(_ship, "_previous", position);
            SetField(_ship, "_current", position);
            _chaserObject.SetActive(true);

            var previousDirection = _chaser.transform.forward;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                Assert.LessOrEqual(_chaser.Position.y, _cylinder.MaxHeight);
                Assert.GreaterOrEqual(_chaser.Position.y, _cylinder.MinHeight);
                float turn = Vector3.Angle(previousDirection, _chaser.transform.forward);
                Assert.Less(turn, 20f, "Chaser visually reversed at the boundary.");
                previousDirection = _chaser.transform.forward;
            }
        }

        private void SetField(string name, object value)
        {
            SetField(_chaser, name, value);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
