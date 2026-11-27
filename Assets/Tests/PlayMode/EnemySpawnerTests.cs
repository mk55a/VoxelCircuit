using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Formations;
using CylinderShooter.Levels;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public sealed class EnemySpawnerTests
    {
        private sealed class AllowAll : ISpawnApproval
        {
            public SpawnVerdict Verdict = SpawnVerdict.Approved;
            public SpawnVerdict Approve(IEnemyType type, int count) => Verdict;
        }

        private readonly List<Object> _objects = new List<Object>();
        private EnemyPool _pool;
        private EnemySpawner _spawner;
        private EnemyDefinition _enemy;
        private EnemyFormationOption _line;
        private ShipController _ship;
        private GameObject _camera;

        [SetUp]
        public void SetUp()
        {
            var cylinder = Make<CylinderConfig>();
            Set(cylinder, "_radius", 60f);
            ServiceLocator.Register(cylinder);
            ServiceLocator.Register<ITargetRegistry>(new TargetRegistry());
            ServiceLocator.Register(new ContactHazardRegistry());

            var shipObject = Track(new GameObject("Ship"));
            shipObject.SetActive(false);
            var input = shipObject.AddComponent<LifeTestInput>();
            var ship = shipObject.AddComponent<ShipController>();
            Set(ship, "_cylinder", cylinder);
            Set(ship, "_tuning", Make<ShipTuning>());
            Set(ship, "_inputSource", input);
            shipObject.SetActive(true);
            ship.enabled = false;
            _ship = ship;

            _camera = Track(new GameObject("Camera"));
            var camera = _camera.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, 82f);
            camera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            camera.aspect = 16f / 9f;
            camera.fieldOfView = 44f;

            var prefabObject = Track(new GameObject("Chaser prefab"));
            prefabObject.SetActive(false);
            var prefab = prefabObject.AddComponent<Chaser>();
            Set(prefab, "_tuning", Make<ChaserTuning>());

            var formation = Make<FormationDefinition>();
            Set(formation, "_shape", FormationShape.HorizontalLine);
            Set(formation, "_count", 3);
            Set(formation, "_detectionRange", 0f);
            _enemy = Make<EnemyDefinition>();
            Set(_enemy, "_prefab", prefab);
            Set(_enemy, "_prewarm", 2);
            _line = new EnemyFormationOption(formation, 3, 3);
            ((List<EnemyFormationOption>)Get(_enemy, "_formations")).Add(_line);

            var poolObject = Track(new GameObject("Pool"));
            var inactive = new GameObject("Inactive");
            inactive.transform.SetParent(poolObject.transform);
            inactive.SetActive(false);
            var active = new GameObject("Active");
            active.transform.SetParent(poolObject.transform);
            _pool = poolObject.AddComponent<EnemyPool>();
            Set(_pool, "_inactiveRoot", inactive.transform);
            Set(_pool, "_activeRoot", active.transform);

            var spawnerObject = Track(new GameObject("Spawner"));
            spawnerObject.SetActive(false);
            _spawner = spawnerObject.AddComponent<EnemySpawner>();
            Set(_spawner, "_ship", ship);
            Set(_spawner, "_camera", camera);
            Set(_spawner, "_pool", _pool);
            Set(_spawner, "_placement", Make<SpawnPlacementProfile>());
            spawnerObject.SetActive(true);
            Assert.IsTrue(_pool.Initialize(new[] { _enemy }));
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            ServiceLocator.Clear();
        }

        /// <summary>A group of 3 in the middle lane, from the screen-left, just off-screen.</summary>
        private SpawnRequest Request(int count = 3, IEntryOption entry = null, int side = -1) =>
            new SpawnRequest(_enemy, _line, count, entry, 0f, side);

        [UnityTest]
        public IEnumerator Spawn_PlacesTheGroupOffScreen_AndCountsIt()
        {
            var outcome = _spawner.TrySpawn(Request(), new AllowAll());
            Assert.AreEqual(SpawnStatus.Spawned, outcome.Status, outcome.Reason);
            Assert.AreEqual(3, outcome.Count);
            Assert.AreEqual(3, _pool.TotalAlive);
            Assert.AreEqual(3, _pool.AliveOf(_enemy));
            yield return null;
            var camera = _camera.GetComponent<Camera>();
            foreach (var chaser in _pool.GetComponentsInChildren<Chaser>())
                Assert.Less(camera.WorldToViewportPoint(chaser.Position).x, 0f, "Entering from the left, off-screen.");
        }

        [UnityTest]
        public IEnumerator KilledEnemies_GoBackToThePool_AndAreReused()
        {
            _spawner.TrySpawn(Request(), new AllowAll());
            yield return null;
            var first = _pool.GetComponentsInChildren<Chaser>();
            first[0].TakeHit(100);
            Assert.AreEqual(2, _pool.AliveOf(_enemy));
            foreach (var chaser in first) chaser.gameObject.SetActive(false);
            Assert.AreEqual(0, _pool.TotalAlive);
            _spawner.TrySpawn(Request(), new AllowAll());
            CollectionAssert.AreEquivalent(first, _pool.GetComponentsInChildren<Chaser>(), "The same three objects are reused.");
        }

        [Test]
        public void RefusedByTheGate_SpawnsNothing()
        {
            var outcome = _spawner.TrySpawn(Request(), new AllowAll { Verdict = SpawnVerdict.NotInStage });
            Assert.AreEqual(SpawnStatus.Refused, outcome.Status);
            Assert.AreEqual(SpawnVerdict.NotInStage, outcome.Verdict);
            Assert.AreEqual(0, _pool.TotalAlive);
        }

        [Test]
        public void AFormationRowWithoutAFormation_IsInvalid()
        {
            var empty = new EnemyFormationOption(null, 3, 3);
            var request = new SpawnRequest(_enemy, empty, 3, null, 0f, -1);
            Assert.AreEqual(SpawnStatus.Invalid, _spawner.TrySpawn(request, new AllowAll()).Status);
        }

        [Test]
        public void TheDirectorsCount_ReplacesTheFormationsOwn()
        {
            var outcome = _spawner.TrySpawn(Request(5), new AllowAll());
            Assert.AreEqual(SpawnStatus.Spawned, outcome.Status, outcome.Reason);
            Assert.AreEqual(5, _pool.AliveOf(_enemy));
        }

        [Test]
        public void AtDistance_PlacesTheGroupThatFarAlongTheRing()
        {
            var far = new EnemyEntryOption(EntryStyle.AtDistance, 120f);
            var outcome = _spawner.TrySpawn(Request(3, far, 1), new AllowAll());
            Assert.AreEqual(SpawnStatus.Spawned, outcome.Status, outcome.Reason);
            foreach (var chaser in _pool.GetComponentsInChildren<Chaser>())
            {
                float metres = Mathf.Abs(CylinderSpace.ShortestDelta(_ship.Position.Angle,
                    Mathf.Atan2(chaser.transform.position.x, chaser.transform.position.z))) * 60f;
                Assert.AreEqual(120f, metres, 5f);
            }
        }

        [Test]
        public void GroupsKeepTheirSpacing()
        {
            var far = new EnemyEntryOption(EntryStyle.AtDistance, 120f);
            _spawner.TrySpawn(Request(3, far, 1), new AllowAll());
            Assert.AreEqual(SpawnStatus.Spawned, _spawner.TrySpawn(Request(3, far, 1), new AllowAll()).Status);
            var centres = new List<float>();
            foreach (var chaser in _pool.GetComponentsInChildren<Chaser>())
                centres.Add(Mathf.Atan2(chaser.transform.position.x, chaser.transform.position.z));
            centres.Sort();
            Assert.Greater((centres[centres.Count - 1] - centres[0]) * 60f, 12f, "the second group moved past the first");
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static FieldInfo Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
        private static object Get(object target, string name) => Field(target, name).GetValue(target);
    }
}
