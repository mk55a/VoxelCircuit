using System.Collections;
using System.Reflection;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Formations;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public class FormationSandboxTests
    {
        private GameObject _shipObject;
        private GameObject _cameraObject;
        private GameObject _prefabObject;
        private GameObject _sandboxObject;
        private GameObject _arenaObject;
        private CylinderConfig _cylinder;
        private ShipTuning _shipTuning;
        private ChaserTuning _chaserTuning;
        private FormationDefinition _definition;
        private FormationTestSpawner _sandbox;

        [SetUp]
        public void SetUp()
        {
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            _shipTuning = ScriptableObject.CreateInstance<ShipTuning>();
            _chaserTuning = ScriptableObject.CreateInstance<ChaserTuning>();
            _definition = ScriptableObject.CreateInstance<FormationDefinition>();
            Set(_cylinder, "_radius", 60f);
            Set(_definition, "_count", 3);
            Set(_definition, "_detectionRange", 0f);
            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(new TargetRegistry());
            ServiceLocator.Register(new ContactHazardRegistry());

            _shipObject = new GameObject("Test ship");
            _shipObject.SetActive(false);
            var input = _shipObject.AddComponent<LifeTestInput>();
            var ship = _shipObject.AddComponent<ShipController>();
            Set(ship, "_cylinder", _cylinder);
            Set(ship, "_tuning", _shipTuning);
            Set(ship, "_inputSource", input);
            _shipObject.SetActive(true);
            ship.enabled = false;

            _cameraObject = new GameObject("Test camera");
            var camera = _cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, 82f);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            camera.aspect = 16f / 9f;
            camera.fieldOfView = 30f;
            _arenaObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _arenaObject.transform.localScale = new Vector3(119f, 8f, 119f);

            _prefabObject = new GameObject("Test Chaser prefab");
            _prefabObject.SetActive(false);
            var prefab = _prefabObject.AddComponent<Chaser>();
            Set(prefab, "_tuning", _chaserTuning);
            _sandboxObject = new GameObject("Sandbox");
            _sandboxObject.SetActive(false);
            _sandbox = _sandboxObject.AddComponent<FormationTestSpawner>();
            Set(_sandbox, "_ship", ship);
            Set(_sandbox, "_camera", camera);
            Set(_sandbox, "_cylinder", _cylinder);
            Set(_sandbox, "_prefab", prefab);
            Set(_sandbox, "_arena", _arenaObject.GetComponent<MeshFilter>());
            Set(_sandbox, "_formation", _definition);
            Set(_sandbox, "_prewarm", 3);
            Set(_sandbox, "_entrySide", FormationEntrySide.Left);
            _sandboxObject.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sandboxObject);
            Object.DestroyImmediate(_prefabObject);
            Object.DestroyImmediate(_cameraObject);
            Object.DestroyImmediate(_arenaObject);
            Object.DestroyImmediate(_shipObject);
            ServiceLocator.Clear();
            Object.DestroyImmediate(_cylinder);
            Object.DestroyImmediate(_shipTuning);
            Object.DestroyImmediate(_chaserTuning);
            Object.DestroyImmediate(_definition);
        }

        [UnityTest]
        public IEnumerator Spawn_ClearAndReuse_ChasersWithoutAffectingRegistry()
        {
            Assert.IsTrue(_sandbox.Spawn());
            Assert.AreEqual(3, _sandbox.ActiveCount);
            var first = _sandbox.GetComponentsInChildren<Chaser>();
            Assert.AreEqual(3, first.Length);
            foreach (var chaser in first)
            {
                Assert.IsTrue(chaser.IsInFormation);
                Assert.Less(_cameraObject.GetComponent<Camera>().WorldToViewportPoint(chaser.Position).x, 0f);
            }
            first[0].TakeHit(2);
            Assert.AreEqual(2, _sandbox.ActiveCount);
            _sandbox.Clear();
            Assert.AreEqual(0, _sandbox.ActiveCount);
            Assert.IsEmpty(ServiceLocator.Get<ITargetRegistry>().Targets);

            Assert.IsTrue(_sandbox.Spawn());
            var second = _sandbox.GetComponentsInChildren<Chaser>();
            CollectionAssert.AreEquivalent(first, second);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Spawn_ReleasesIndividualsWhenPlayerEntersRange()
        {
            Set(_definition, "_detectionRange", 5f);
            Assert.IsTrue(_sandbox.Spawn());
            var chaser = _sandbox.GetComponentsInChildren<Chaser>()[0];
            var ship = _shipObject.GetComponent<ShipController>();
            Set(ship, "_previous", chaser.ContactPosition);
            Set(ship, "_current", chaser.ContactPosition);
            yield return null;
            Assert.IsFalse(chaser.IsInFormation);
            _sandbox.Clear();
            Assert.AreEqual(0, _sandbox.ActiveCount);
        }

        [UnityTest]
        public IEnumerator WideView_UsesOpaqueArenaToHideTwentyFiveMembers()
        {
            _cameraObject.GetComponent<Camera>().fieldOfView = 60f;
            Set(_definition, "_count", 25);
            Assert.IsTrue(_sandbox.Spawn());
            Assert.AreEqual(25, _sandbox.ActiveCount);
            _sandbox.Clear();
            yield return null;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
