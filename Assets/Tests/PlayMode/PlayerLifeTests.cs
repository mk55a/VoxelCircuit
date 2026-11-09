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
    public sealed class LifeTestInput : MonoBehaviour, IShipInput, IFireInput
    {
        public Vector2 Move => Vector2.zero;
        public bool ConsumeBoost() => false;
        public bool FireHeld => false;
    }

    public class PlayerLifeTests
    {
        private GameObject _shipObject;
        private GameObject _enemyObject;
        private GameObject _secondEnemyObject;
        private GameObject _cannonObject;
        private CylinderConfig _cylinder;
        private PlayerLifeTuning _lifeTuning;
        private ShipTuning _shipTuning;
        private DrifterTuning _drifterTuning;
        private ChaserTuning _chaserTuning;
        private TargetRegistry _targets;
        private ContactHazardRegistry _hazards;
        private PlayerLife _life;
        private ShipController _ship;
        private GameObject _visual;
        private WeaponTuning _weaponTuning;
        private TestBulletPool _pool;

        private sealed class TestBulletPool : IBulletPool
        {
            public Bullet Get()
            {
                Assert.Fail("A destroyed ship must not request a bullet.");
                return null;
            }
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _cylinder = ScriptableObject.CreateInstance<CylinderConfig>();
            _lifeTuning = ScriptableObject.CreateInstance<PlayerLifeTuning>();
            _shipTuning = ScriptableObject.CreateInstance<ShipTuning>();
            _drifterTuning = ScriptableObject.CreateInstance<DrifterTuning>();
            _chaserTuning = ScriptableObject.CreateInstance<ChaserTuning>();
            _weaponTuning = ScriptableObject.CreateInstance<WeaponTuning>();
            _pool = new TestBulletPool();
            SetField(_lifeTuning, "_respawnDelay", 0.1f);
            SetField(_lifeTuning, "_invulnerabilityDuration", 0.5f);
            _targets = new TargetRegistry();
            _hazards = new ContactHazardRegistry();
            ServiceLocator.Register(_cylinder);
            ServiceLocator.Register<ITargetRegistry>(_targets);
            ServiceLocator.Register(_hazards);
            ServiceLocator.Register<IBulletPool>(_pool);
            _shipObject = new GameObject("Test ship");
            _shipObject.SetActive(false);
            var input = _shipObject.AddComponent<LifeTestInput>();
            _ship = _shipObject.AddComponent<ShipController>();
            _life = _shipObject.AddComponent<PlayerLife>();
            var contact = _shipObject.AddComponent<PlayerContact>();
            _visual = new GameObject("Visual");
            _visual.transform.SetParent(_shipObject.transform);
            SetField(_ship, "_cylinder", _cylinder);
            SetField(_ship, "_tuning", _shipTuning);
            SetField(_ship, "_inputSource", input);
            SetField(_ship, "_life", _life);
            SetField(_life, "_tuning", _lifeTuning);
            SetField(_life, "_ship", _ship);
            SetField(_life, "_visual", _visual);
            SetField(contact, "_ship", _ship);
            SetField(contact, "_life", _life);
            SetField(contact, "_cylinder", _cylinder);
            _shipObject.SetActive(true);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyObject);
            Object.DestroyImmediate(_secondEnemyObject);
            Object.DestroyImmediate(_shipObject);
            Object.DestroyImmediate(_cannonObject);
            ServiceLocator.Unregister(_cylinder);
            ServiceLocator.Unregister<ITargetRegistry>(_targets);
            ServiceLocator.Unregister(_hazards);
            ServiceLocator.Unregister<IBulletPool>(_pool);
            Object.DestroyImmediate(_cylinder);
            Object.DestroyImmediate(_lifeTuning);
            Object.DestroyImmediate(_shipTuning);
            Object.DestroyImmediate(_drifterTuning);
            Object.DestroyImmediate(_chaserTuning);
            Object.DestroyImmediate(_weaponTuning);
        }

        [UnityTest]
        public IEnumerator Contact_RespawnsAtDeathPositionAndDestroysInvulnerableEnemies()
        {
            _enemyObject = CreateDrifter();
            yield return null;
            Assert.AreEqual(PlayerStatus.Respawning, _life.Status);
            Assert.AreEqual(2, _life.Lives);
            Assert.IsFalse(_enemyObject.activeSelf);
            Assert.IsFalse(_visual.activeSelf);
            Vector3 deathPosition = _ship.Position.ToWorld(_cylinder.Radius);
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(PlayerStatus.Invulnerable, _life.Status);
            Assert.IsTrue(_visual.activeSelf);
            Assert.That(Vector3.Distance(deathPosition, _ship.Position.ToWorld(_cylinder.Radius)), Is.LessThan(0.001f));
            Assert.AreEqual(0f, _ship.HorizontalVelocity, 0.001f);

            _secondEnemyObject = CreateChaser();
            yield return null;
            Assert.IsFalse(_secondEnemyObject.activeSelf);
            Assert.AreEqual(2, _life.Lives);
            Assert.AreEqual(PlayerStatus.Invulnerable, _life.Status);
        }

        [UnityTest]
        public IEnumerator SimultaneousContacts_ConsumeOnlyOneLife()
        {
            _enemyObject = CreateDrifter();
            _secondEnemyObject = CreateChaser();
            yield return null;
            Assert.AreEqual(2, _life.Lives);
            Assert.AreEqual(PlayerStatus.Respawning, _life.Status);
            Assert.IsFalse(_enemyObject.activeSelf);
            Assert.IsFalse(_secondEnemyObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator DummyContact_DoesNotDamagePlayer()
        {
            _enemyObject = new GameObject("Dummy");
            _enemyObject.SetActive(false);
            var renderer = _enemyObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var dummy = _enemyObject.AddComponent<TargetDummy>();
            SetField(dummy, "_body", renderer);
            _enemyObject.SetActive(true);
            yield return null;
            Assert.AreEqual(PlayerStatus.Alive, _life.Status);
            Assert.AreEqual(3, _life.Lives);
            Assert.IsTrue(dummy.IsAlive);
            Object.DestroyImmediate(renderer.sharedMaterial);
        }

        [UnityTest]
        public IEnumerator DeathDuringBoost_PausesFullCooldownUntilRespawn()
        {
            var boost = (BoostAbility)typeof(ShipController)
                .GetField("_boost", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ship);
            Assert.IsTrue(boost.TryActivate());
            _life.TryDestroy();
            Assert.IsFalse(boost.IsActive);
            float cooldown = boost.CooldownRemaining;
            Assert.AreEqual(_shipTuning.BoostCooldown, cooldown, 0.001f);
            yield return new WaitForSeconds(0.06f);
            Assert.AreEqual(PlayerStatus.Respawning, _life.Status);
            Assert.AreEqual(cooldown, boost.CooldownRemaining, 0.001f);
            yield return new WaitForSeconds(0.15f);
            Assert.Less(boost.CooldownRemaining, cooldown);
            Assert.AreEqual(0f, _ship.HorizontalVelocity, 0.001f);
        }

        [TestCase(0, 0f)]
        [TestCase(1, 90f)]
        [TestCase(2, 180f)]
        public void BoostRoll_UsesConfiguredCountAndReturnsToBank(int rolls, float quarterAngle)
        {
            SetField(_shipTuning, "_boostRollCount", rolls);
            SetField(_shipTuning, "_maxBankAngle", 0f);
            var banking = _shipObject.AddComponent<ShipBanking>();
            banking.enabled = false;
            SetField(banking, "_ship", _ship);
            SetField(banking, "_model", _visual.transform);
            var boost = (BoostAbility)typeof(ShipController)
                .GetField("_boost", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ship);
            Assert.IsTrue(boost.TryActivate());
            boost.Tick(_shipTuning.BoostDuration * 0.25f);
            banking.SendMessage("LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.Euler(0f, 0f, quarterAngle), _visual.transform.localRotation), 0.01f);
            boost.Tick(_shipTuning.BoostDuration);
            banking.SendMessage("LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.identity, _visual.transform.localRotation), 0.01f);
        }

        [Test]
        public void InterruptedBoost_RestoresModelBank()
        {
            SetField(_shipTuning, "_maxBankAngle", 0f);
            var banking = _shipObject.AddComponent<ShipBanking>();
            banking.enabled = false;
            SetField(banking, "_ship", _ship);
            SetField(banking, "_model", _visual.transform);
            var boost = (BoostAbility)typeof(ShipController)
                .GetField("_boost", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ship);
            Assert.IsTrue(boost.TryActivate());
            boost.Tick(_shipTuning.BoostDuration * 0.25f);
            banking.SendMessage("LateUpdate");
            Assert.Greater(Quaternion.Angle(Quaternion.identity, _visual.transform.localRotation), 80f);
            _life.TryDestroy();
            banking.SendMessage("LateUpdate");
            Assert.Less(Quaternion.Angle(Quaternion.identity, _visual.transform.localRotation), 0.01f);
        }

        [Test]
        public void DestroyedShip_CannotFire()
        {
            _cannonObject = new GameObject("Test cannon");
            _cannonObject.SetActive(false);
            var input = _cannonObject.AddComponent<LifeTestInput>();
            var cannon = _cannonObject.AddComponent<MainCannon>();
            SetField(cannon, "_tuning", _weaponTuning);
            SetField(cannon, "_aimSource", _ship);
            SetField(cannon, "_fireInput", input);
            SetField(cannon, "_life", _life);
            _cannonObject.SetActive(true);
            _life.TryDestroy();
            Assert.IsFalse(cannon.TryFire());
        }

        private GameObject CreateDrifter()
        {
            var enemy = new GameObject("Drifter");
            enemy.SetActive(false);
            var drifter = enemy.AddComponent<Drifter>();
            SetField(drifter, "_tuning", _drifterTuning);
            enemy.SetActive(true);
            return enemy;
        }

        private GameObject CreateChaser()
        {
            var enemy = new GameObject("Chaser");
            enemy.SetActive(false);
            var chaser = enemy.AddComponent<Chaser>();
            SetField(chaser, "_tuning", _chaserTuning);
            SetField(chaser, "_target", _ship);
            enemy.SetActive(true);
            return enemy;
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
