using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CylinderShooter.Core;
using CylinderShooter.Enemies;
using CylinderShooter.Formations;
using CylinderShooter.Ship;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CylinderShooter.Tests
{
    public sealed class BomberTests
    {
        private const float Radius = 60f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private CylinderConfig _cylinder;
        private ShipTuning _shipTuning;
        private PlayerLifeTuning _lifeTuning;
        private BomberTuning _bomberTuning;
        private FormationDefinition _definition;
        private ShipController _ship;
        private PlayerLife _life;
        private int _detonations;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            _shipTuning = ScriptableObject.CreateInstance<ShipTuning>();
            _lifeTuning = ScriptableObject.CreateInstance<PlayerLifeTuning>();
            _bomberTuning = ScriptableObject.CreateInstance<BomberTuning>();
            _definition = ScriptableObject.CreateInstance<FormationDefinition>();
            Set(_cylinder, "_radius", Radius);
            Set(_definition, "_shape", FormationShape.HorizontalLine);
            Set(_definition, "_count", 3);
            Set(_definition, "_horizontalSpacing", 1.8f);
            Set(_definition, "_speed", 0f);
            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(new TargetRegistry());
            ServiceLocator.Register(new ContactHazardRegistry());

            var shipObject = Track(new GameObject("Test ship"));
            shipObject.SetActive(false);
            var input = shipObject.AddComponent<LifeTestInput>();
            _ship = shipObject.AddComponent<ShipController>();
            _life = shipObject.AddComponent<PlayerLife>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(shipObject.transform);
            Set(_ship, "_cylinder", _cylinder);
            Set(_ship, "_tuning", _shipTuning);
            Set(_ship, "_inputSource", input);
            Set(_ship, "_life", _life);
            Set(_life, "_tuning", _lifeTuning);
            Set(_life, "_ship", _ship);
            Set(_life, "_visual", visual);
            shipObject.SetActive(true);
            _ship.enabled = false;
            PlacePlayer(Mathf.PI);   // far away until a test moves the player
            _detonations = 0;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            ServiceLocator.Clear();
            Object.DestroyImmediate(_cylinder);
            Object.DestroyImmediate(_shipTuning);
            Object.DestroyImmediate(_lifeTuning);
            Object.DestroyImmediate(_bomberTuning);
            Object.DestroyImmediate(_definition);
        }

        [UnityTest]
        public IEnumerator ShotWhileIdle_PopsWithoutBlast()
        {
            var mines = SpawnFormation(0f, 0f);
            bool destroyed = false;
            mines[1].Destroyed += _ => destroyed = true;
            mines[1].TakeHit(1);
            yield return null;
            Assert.IsTrue(destroyed);
            Assert.AreEqual(0, _detonations);
            Assert.IsFalse(mines[1].gameObject.activeSelf);
            Assert.IsTrue(mines[0].gameObject.activeSelf);
            Assert.IsTrue(mines[2].gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator FuseRunsOut_BlastDestroysPlayerAndFormationMates()
        {
            var mines = SpawnFormation(0f, 0f);
            // Slot 0 sits 1.8 m to the left (higher angle); the player is 4 m beyond it, out of the others' range.
            PlacePlayer(mines[0].ContactPosition.Angle + 4f / Radius);
            int lives = _life.Lives;
            yield return null;
            Assert.IsTrue(mines[0].IsArmed);
            Assert.IsFalse(mines[1].IsArmed);

            yield return new WaitForSeconds(_bomberTuning.FuseDuration + 0.2f);
            Assert.AreEqual(1, _detonations);
            Assert.AreEqual(lives - 1, _life.Lives);
            foreach (var mine in mines) Assert.IsFalse(mine.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator ShotWhileArmed_ChainsThroughOwnFormationOnly_AndSparesPlayer()
        {
            var mines = SpawnFormation(0f, 0f);
            var stranger = SpawnFormation(0f, 2.5f)[1];   // another formation, inside the blast
            PlacePlayer(mines[0].ContactPosition.Angle + 4f / Radius);
            int lives = _life.Lives;
            yield return null;
            Assert.IsTrue(mines[0].IsArmed);

            mines[0].TakeHit(1);
            yield return null;
            Assert.GreaterOrEqual(_detonations, 1);
            Assert.AreEqual(lives, _life.Lives, "A shot chain reaction must not hurt the player by default.");
            Assert.IsFalse(mines[1].gameObject.activeSelf, "Formation mate inside the blast is destroyed.");
            Assert.IsTrue(stranger.gameObject.activeSelf, "Enemies of another formation are not hurt.");
        }

        private Bomber[] SpawnFormation(float angle, float height)
        {
            var motion = new FormationMotion(_definition, new CylinderPosition(angle, height), Radius, 1);
            var mines = new Bomber[_definition.Count];
            for (int i = 0; i < mines.Length; i++)
            {
                var go = Track(new GameObject($"Bomber {i}"));
                go.SetActive(false);
                var bomber = go.AddComponent<Bomber>();
                Set(bomber, "_tuning", _bomberTuning);
                bomber.PrepareSpawn(_ship, motion, i, _ => { });
                bomber.Detonated += _ => _detonations++;
                go.SetActive(true);
                mines[i] = bomber;
            }
            return mines;
        }

        private void PlacePlayer(float angle)
        {
            var position = new CylinderPosition(CylinderSpace.WrapAngle(angle), 0f);
            Set(_ship, "_previous", position);
            Set(_ship, "_current", position);
        }

        private GameObject Track(GameObject go)
        {
            _objects.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
