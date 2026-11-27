using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CylinderShooter.Combat;
using CylinderShooter.Core;
using CylinderShooter.Debugging;
using CylinderShooter.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public sealed class LevelManagerTests
    {
        private sealed class FakeEnemy : MonoBehaviour, IContactHazard
        {
            public ContactHazardRegistry Registry;
            public bool IsAlive => true;
            public float Radius => 1f;
            public CylinderPosition PreviousContactPosition => default;
            public CylinderPosition ContactPosition => default;
            public void DestroyOnContact() { }
            private void OnDisable() => Registry?.Unregister(this);
        }

        private readonly List<Object> _objects = new List<Object>();
        private StageDefinition _first;
        private StageDefinition _second;
        private GameObject _game;
        private LevelManager _manager;

        [SetUp]
        public void SetUp()
        {
            _first = Make<StageDefinition>();
            _second = Make<StageDefinition>();
            Set(_first, "_duration", 0.2f);
            Set(_second, "_duration", 100f);
            var wave = Make<StandardWaveDefinition>();
            ((List<StageDefinition>)Get(wave, "_stages")).AddRange(new[] { _first, _second });
            var level = Make<LevelDefinition>();
            ((List<WaveDefinition>)Get(level, "_waves")).Add(wave);
            var cylinder = Make<CylinderConfig>();

            _game = new GameObject("Game");
            _objects.Add(_game);
            _game.SetActive(false);
            var bulletPrefab = new GameObject("Bullet");
            bulletPrefab.SetActive(false);
            _objects.Add(bulletPrefab);
            var pool = _game.AddComponent<BulletPool>();
            Set(pool, "_prefab", bulletPrefab.AddComponent<Bullet>());
            _manager = _game.AddComponent<LevelManager>();
            Set(_manager, "_level", level);
            var installer = _game.AddComponent<GameInstaller>();
            Set(installer, "_cylinder", cylinder);
            Set(installer, "_bulletPool", pool);
            Set(installer, "_levelManager", _manager);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            ServiceLocator.Clear();
        }

        [UnityTest]
        public IEnumerator Level_StartsAndMovesToTheNextStage()
        {
            _game.SetActive(true);
            yield return null;
            Assert.IsTrue(ServiceLocator.TryGet<IRunEvents>(out var events));
            Assert.AreSame(_manager.Run, events);
            Assert.AreSame(_first, _manager.Run.CurrentStage.Settings);
            yield return new WaitForSeconds(0.4f);
            Assert.AreSame(_second, _manager.Run.CurrentStage.Settings);
        }

        [UnityTest]
        public IEnumerator Debug_PauseStopsTheClock_AndSkipRemovesEnemies()
        {
            _game.SetActive(true);
            yield return null;
            var debug = ServiceLocator.Get<IRunDebug>();
            debug.IsPaused = true;
            float before = debug.Run.RunTime;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(before, debug.Run.RunTime);

            var registry = ServiceLocator.Get<ContactHazardRegistry>();
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Enemy");
                _objects.Add(go);
                var enemy = go.AddComponent<FakeEnemy>();
                enemy.Registry = registry;
                registry.Register(enemy);
            }
            Assert.AreEqual(3, debug.AliveEnemies);
            Assert.IsTrue(debug.SkipStage());
            Assert.AreEqual(0, debug.AliveEnemies);
            Assert.AreSame(_second, debug.Run.CurrentStage.Settings);
        }

        [UnityTest]
        public IEnumerator Services_AreRemovedWithTheScene()
        {
            _game.SetActive(true);
            yield return null;
            Object.DestroyImmediate(_game);
            Assert.IsFalse(ServiceLocator.TryGet<IRunEvents>(out _));
            Assert.IsFalse(ServiceLocator.TryGet<IRunDebug>(out _));
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private static FieldInfo Field(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
        private static object Get(object target, string name) => Field(target, name).GetValue(target);
    }
}
